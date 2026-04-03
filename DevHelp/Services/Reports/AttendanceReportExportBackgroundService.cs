using DevHelp.Data;
using DevHelp.Models.Tickets;
using DevHelp.Services.Email;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace DevHelp.Services.Reports
{
    // Processa em segundo plano exportações pendentes de histórico de atendimentos em PDF.
    public class AttendanceReportExportBackgroundService(
        IServiceScopeFactory scopeFactory,
        IWebHostEnvironment environment,
        ILogger<AttendanceReportExportBackgroundService> logger) : BackgroundService
    {
        // Cria escopos DI para resolver serviços scoped por iteração.
        private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
        // Ambiente da aplicação para resolução de paths de arquivos gerados.
        private readonly IWebHostEnvironment _environment = environment;
        // Logger para diagnóstico de falhas no processamento assíncrono.
        private readonly ILogger<AttendanceReportExportBackgroundService> _logger = logger;

        // Mantém loop contínuo verificando jobs pendentes de exportação.
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessNextPendingJobAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Falha inesperada no worker de exportação de histórico.");
                }

                await Task.Delay(TimeSpan.FromSeconds(4), stoppingToken);
            }
        }

        // Seleciona e processa um job pendente por vez para controle de concorrência.
        private async Task ProcessNextPendingJobAsync(CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<Models.Identity.ApplicationUser>>();
            var emailSender = scope.ServiceProvider.GetRequiredService<IAppEmailSender>();

            var job = await dbContext.AttendanceReportExportJobs
                .OrderBy(j => j.CreatedAtUtc)
                .FirstOrDefaultAsync(j => j.Status == AttendanceReportExportStatus.Pending, cancellationToken);

            if (job is null)
            {
                return;
            }

            job.Status = AttendanceReportExportStatus.Processing;
            job.StartedAtUtc = DateTime.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);

            try
            {
                var itemsQuery = dbContext.Tickets
                    .AsNoTracking()
                    .Where(t => (t.Status == TicketStatus.Resolved || t.Status == TicketStatus.Closed || t.Status == TicketStatus.Cancelled)
                             && t.ServiceFinishedAtUtc != null);

                if (!string.IsNullOrWhiteSpace(job.SearchTerm))
                {
                    var normalized = job.SearchTerm.Trim();
                    itemsQuery = itemsQuery.Where(t =>
                        (t.Student != null && (
                            (t.Student.FullName != null && t.Student.FullName.Contains(normalized))
                            || (t.Student.Email != null && t.Student.Email.Contains(normalized))))
                        || (t.AssignedProfessor != null && (
                            (t.AssignedProfessor.FullName != null && t.AssignedProfessor.FullName.Contains(normalized))
                            || (t.AssignedProfessor.Email != null && t.AssignedProfessor.Email.Contains(normalized))))
                        || t.TicketNumber.Contains(normalized));
                }

                if (job.CategoryId.HasValue)
                {
                    itemsQuery = itemsQuery.Where(t => t.CategoryId == job.CategoryId.Value);
                }

                if (!string.IsNullOrWhiteSpace(job.PriorityFilter)
                    && Enum.TryParse<TicketPriority>(job.PriorityFilter, true, out var parsedPriority))
                {
                    itemsQuery = itemsQuery.Where(t => t.Priority == parsedPriority);
                }

                if (job.DateFromUtc.HasValue)
                {
                    itemsQuery = itemsQuery.Where(t => t.ServiceFinishedAtUtc >= job.DateFromUtc.Value);
                }

                if (job.DateToUtc.HasValue)
                {
                    itemsQuery = itemsQuery.Where(t => t.ServiceFinishedAtUtc <= job.DateToUtc.Value);
                }

                var items = await itemsQuery
                    .OrderByDescending(t => t.ServiceFinishedAtUtc)
                    .Select(t => new AttendanceReportPdfRow
                    {
                        TicketNumber = t.TicketNumber,
                        StudentName = t.Student != null ? (t.Student.FullName ?? t.Student.Email ?? "Aluno") : "Aluno",
                        ProfessorName = t.AssignedProfessor != null ? (t.AssignedProfessor.FullName ?? t.AssignedProfessor.Email ?? "Docente") : "Docente não informado",
                        CategoryName = t.Category != null ? t.Category.Name : "-",
                        Priority = t.Priority,
                        ServiceFinishedAtUtc = t.ServiceFinishedAtUtc!.Value,
                        TotalMinutes = t.ServiceStartedAtUtc != null
                            ? EF.Functions.DateDiffMinute(t.ServiceStartedAtUtc.Value, t.ServiceFinishedAtUtc!.Value)
                            : EF.Functions.DateDiffMinute(t.CreatedAtUtc, t.ServiceFinishedAtUtc!.Value)
                    })
                    .ToListAsync(cancellationToken);

                var generatedAt = DateTime.UtcNow;
                var folder = Path.Combine(_environment.ContentRootPath, "App_Data", "reports");
                Directory.CreateDirectory(folder);

                var fileName = $"historico-atendimentos-{job.Id:D6}.pdf";
                var fullPath = Path.Combine(folder, fileName);

                GeneratePdf(fullPath, items, generatedAt, job);

                job.OutputFileName = fileName;
                job.RelativePath = fileName;
                job.Status = AttendanceReportExportStatus.Completed;
                job.CompletedAtUtc = DateTime.UtcNow;
                job.ErrorMessage = null;
                await dbContext.SaveChangesAsync(cancellationToken);

                var requester = await userManager.FindByIdAsync(job.RequesterId);
                if (requester is not null && !string.IsNullOrWhiteSpace(requester.Email))
                {
                    var subject = "DevHelp - Relatório de histórico de atendimentos pronto";
                    var html = "<p>Seu relatório em PDF foi processado com sucesso no DevHelp.</p><p>Você também pode baixar o arquivo na tela de histórico de atendimentos.</p>";
                    var attachment = new EmailAttachment
                    {
                        FileName = fileName,
                        ContentType = "application/pdf",
                        Content = await File.ReadAllBytesAsync(fullPath, cancellationToken)
                    };

                    await emailSender.SendEmailAsync(requester.Email, subject, html, [attachment]);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falha ao processar exportação de relatório (JobId: {JobId}).", job.Id);
                job.Status = AttendanceReportExportStatus.Failed;
                job.CompletedAtUtc = DateTime.UtcNow;
                job.ErrorMessage = ex.Message.Length > 1000 ? ex.Message[..1000] : ex.Message;
                await dbContext.SaveChangesAsync(cancellationToken);
            }
        }

        // Gera arquivo PDF do relatório de atendimentos conforme filtros da solicitação.
        private static void GeneratePdf(string filePath, IReadOnlyCollection<AttendanceReportPdfRow> items, DateTime generatedAtUtc, AttendanceReportExportJob job)
        {
            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(24);
                    page.Size(PageSizes.A4.Landscape());
                    page.DefaultTextStyle(x => x.FontSize(10));

                    page.Header().Column(column =>
                    {
                        column.Item().Text("DevHelp - Histórico de Atendimentos").SemiBold().FontSize(18);
                        column.Item().Text($"Gerado em: {generatedAtUtc.ToLocalTime():dd/MM/yyyy HH:mm}");
                        column.Item().Text($"Filtros: Busca={job.SearchTerm ?? "-"} | Categoria={job.CategoryId?.ToString() ?? "Todas"} | Prioridade={job.PriorityFilter ?? "Todas"} | De={job.DateFromUtc?.ToLocalTime().ToString("dd/MM/yyyy") ?? "-"} | Até={job.DateToUtc?.ToLocalTime().ToString("dd/MM/yyyy") ?? "-"}");
                    });

                    page.Content().PaddingTop(12).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(3);
                            columns.RelativeColumn(3);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(1);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(1);
                        });

                        table.Header(header =>
                        {
                            static IContainer HeaderStyle(IContainer x) => x.BorderBottom(1).BorderColor(Colors.Grey.Lighten1).PaddingVertical(4);

                            header.Cell().Element(HeaderStyle).Text("Ticket").SemiBold();
                            header.Cell().Element(HeaderStyle).Text("Aluno").SemiBold();
                            header.Cell().Element(HeaderStyle).Text("Docente").SemiBold();
                            header.Cell().Element(HeaderStyle).Text("Categoria").SemiBold();
                            header.Cell().Element(HeaderStyle).Text("Prioridade").SemiBold();
                            header.Cell().Element(HeaderStyle).Text("Fim").SemiBold();
                            header.Cell().Element(HeaderStyle).Text("Tempo").SemiBold();
                        });

                        foreach (var item in items)
                        {
                            static IContainer CellStyle(IContainer x) => x.BorderBottom(1).BorderColor(Colors.Grey.Lighten3).PaddingVertical(3);

                            table.Cell().Element(CellStyle).Text(item.TicketNumber);
                            table.Cell().Element(CellStyle).Text(item.StudentName);
                            table.Cell().Element(CellStyle).Text(item.ProfessorName);
                            table.Cell().Element(CellStyle).Text(item.CategoryName);
                            table.Cell().Element(CellStyle).Text(PriorityToPtBr(item.Priority));
                            table.Cell().Element(CellStyle).Text(item.ServiceFinishedAtUtc.ToLocalTime().ToString("dd/MM/yyyy HH:mm"));
                            table.Cell().Element(CellStyle).Text($"{Math.Max(0, item.TotalMinutes ?? 0)} min");
                        }
                    });

                    page.Footer().AlignRight().Text(x =>
                    {
                        x.Span("Página ");
                        x.CurrentPageNumber();
                        x.Span(" de ");
                        x.TotalPages();
                    });
                });
            }).GeneratePdf(filePath);
        }

        // Converte enum de prioridade para texto amigável em pt-BR no PDF.
        private static string PriorityToPtBr(TicketPriority priority)
        {
            return priority switch
            {
                TicketPriority.Urgent => "Urgente",
                TicketPriority.High => "Alta",
                TicketPriority.Medium => "Média",
                _ => "Baixa"
            };
        }

        // Representa uma linha consolidada para composição do relatório PDF.
        private sealed class AttendanceReportPdfRow
        {
            public string TicketNumber { get; set; } = string.Empty;
            public string StudentName { get; set; } = string.Empty;
            public string ProfessorName { get; set; } = string.Empty;
            public string CategoryName { get; set; } = string.Empty;
            public TicketPriority Priority { get; set; }
            public DateTime ServiceFinishedAtUtc { get; set; }
            public int? TotalMinutes { get; set; }
        }
    }
}
