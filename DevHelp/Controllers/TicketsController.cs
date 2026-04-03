using DevHelp.Data;
using DevHelp.Models.Identity;
using DevHelp.Models.Tickets;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace DevHelp.Controllers
{
    // Gerencia abertura e consulta de chamados pelos perfis autorizados.
    [Authorize]
    public class TicketsController(
        ApplicationDbContext dbContext,
        UserManager<ApplicationUser> userManager,
        IWebHostEnvironment environment) : Controller
    {
        // Acesso ao banco para persistência e leitura de chamados.
        private readonly ApplicationDbContext _dbContext = dbContext;
        // Gerenciador de usuários para identificar o aluno autenticado.
        private readonly UserManager<ApplicationUser> _userManager = userManager;
        // Ambiente web para resolver diretório de upload em wwwroot.
        private readonly IWebHostEnvironment _environment = environment;

        // Exibe formulário de abertura de chamado para alunos.
        [Authorize(Roles = "Aluno")]
        public async Task<IActionResult> Create()
        {
            var model = new TicketCreateViewModel
            {
                CategoryOptions = await BuildCategoryOptionsAsync(),
                PriorityOptions = BuildPriorityOptions(),
                ProfessorOptions = await BuildProfessorOptionsAsync()
            };

            return View(model);
        }

        // Processa abertura do chamado com geração automática de número.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Aluno")]
        public async Task<IActionResult> Create(TicketCreateViewModel input)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user is null)
            {
                return Challenge();
            }

            if (input.CategoryId is not null)
            {
                var categoryExists = await _dbContext.Categories.AnyAsync(c => c.Id == input.CategoryId.Value);
                if (!categoryExists)
                {
                    ModelState.AddModelError(nameof(input.CategoryId), "Categoria inválida.");
                }
            }

            var parsedLinks = ParseLinks(input.Links);
            if (parsedLinks is null)
            {
                ModelState.AddModelError(nameof(input.Links), "Informe links válidos começando com http:// ou https://.");
            }

            if (!string.IsNullOrWhiteSpace(input.PreferredProfessorId))
            {
                var preferredProfessor = await _userManager.FindByIdAsync(input.PreferredProfessorId);
                var isProfessor = preferredProfessor is not null && await _userManager.IsInRoleAsync(preferredProfessor, "Professor");
                if (!isProfessor)
                {
                    ModelState.AddModelError(nameof(input.PreferredProfessorId), "Professor selecionado é inválido.");
                }
            }

            if (!ModelState.IsValid)
            {
                input.CategoryOptions = await BuildCategoryOptionsAsync();
                input.PriorityOptions = BuildPriorityOptions();
                input.ProfessorOptions = await BuildProfessorOptionsAsync();
                return View(input);
            }

            var openedAtUtc = DateTime.UtcNow;
            var ticket = new Ticket
            {
                TicketNumber = await GenerateTicketNumberAsync(openedAtUtc),
                StudentId = user.Id,
                CategoryId = input.CategoryId!.Value,
                Priority = input.Priority!.Value,
                Description = input.Description.Trim(),
                CreatedAtUtc = openedAtUtc,
                ResponseDueAtUtc = CalculateResponseDueAtUtc(openedAtUtc, input.Priority.Value),
                PreferredProfessorId = string.IsNullOrWhiteSpace(input.PreferredProfessorId) ? null : input.PreferredProfessorId
            };

            _dbContext.Tickets.Add(ticket);
            await _dbContext.SaveChangesAsync();

            await SaveAttachmentsAsync(ticket, input.Files ?? []);

            if (parsedLinks is not null)
            {
                foreach (var link in parsedLinks)
                {
                    _dbContext.TicketAttachments.Add(new TicketAttachment
                    {
                        TicketId = ticket.Id,
                        ExternalUrl = link
                    });
                }
            }

            if (_dbContext.ChangeTracker.HasChanges())
            {
                await _dbContext.SaveChangesAsync();
            }

            TempData["TicketSuccess"] = $"Chamado {ticket.TicketNumber} aberto com sucesso.";
            return RedirectToAction(nameof(My));
        }

        // Exibe os chamados do aluno autenticado por ordem de abertura mais recente.
        [Authorize(Roles = "Aluno")]
        public async Task<IActionResult> My()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user is null)
            {
                return Challenge();
            }

            var tickets = await _dbContext.Tickets
                .AsNoTracking()
                .Where(t => t.StudentId == user.Id)
                .OrderByDescending(t => t.CreatedAtUtc)
                .Select(t => new TicketListItemViewModel
                {
                    Id = t.Id,
                    TicketNumber = t.TicketNumber,
                    CategoryName = t.Category != null ? t.Category.Name : "-",
                    Priority = t.Priority,
                    Status = t.Status,
                    CreatedAtUtc = t.CreatedAtUtc,
                    ResponseDueAtUtc = t.ResponseDueAtUtc,
                    AttachmentsCount = t.Attachments.Count,
                    PreferredProfessorName = t.PreferredProfessor != null
                        ? (t.PreferredProfessor.FullName ?? t.PreferredProfessor.Email)
                        : null
                })
                .ToListAsync();

            return View(tickets);
        }

        // Exibe fila de atendimento combinando prioridade e tempo de espera.
        [Authorize(Roles = "Professor,Admin")]
        public async Task<IActionResult> Queue()
        {
            var currentProfessor = await _userManager.GetUserAsync(User);
            if (currentProfessor is null)
            {
                return Challenge();
            }

            var isAdmin = User.IsInRole("Admin");
            var nowUtc = DateTime.UtcNow;
            const int priorityBoostMinutes = 240;

            var queue = await _dbContext.Tickets
                .AsNoTracking()
                .Where(t => t.Status != TicketStatus.Resolved && t.Status != TicketStatus.Closed && t.Status != TicketStatus.Cancelled)
                .Select(t => new TicketQueueItemViewModel
                {
                    Id = t.Id,
                    TicketNumber = t.TicketNumber,
                    CategoryName = t.Category != null ? t.Category.Name : "-",
                    Priority = t.Priority,
                    Status = t.Status,
                    CreatedAtUtc = t.CreatedAtUtc,
                    ResponseDueAtUtc = t.ResponseDueAtUtc,
                    AttachmentsCount = t.Attachments.Count,
                    StudentName = t.Student != null
                        ? (t.Student.FullName ?? t.Student.Email ?? "Aluno")
                        : "Aluno",
                    QueueScore = EF.Functions.DateDiffMinute(t.CreatedAtUtc, nowUtc) + ((int)t.Priority * priorityBoostMinutes),
                    IsOverdue = EF.Functions.DateDiffMinute(t.ResponseDueAtUtc, nowUtc) > 0,
                    OverdueMinutes = EF.Functions.DateDiffMinute(t.ResponseDueAtUtc, nowUtc),
                    IsReservedForAnotherProfessor = !isAdmin
                        && t.PreferredProfessorId != null
                        && t.PreferredProfessorId != currentProfessor.Id,
                    PreferredProfessorName = t.PreferredProfessor != null
                        ? (t.PreferredProfessor.FullName ?? t.PreferredProfessor.Email)
                        : null
                })
                .OrderByDescending(t => t.QueueScore)
                .ThenBy(t => t.CreatedAtUtc)
                .ToListAsync();

            return View(queue);
        }

        // Exibe histórico geral de atendimentos finalizados com filtros e paginação.
        [Authorize(Roles = "Professor,Admin")]
        public async Task<IActionResult> History(string? searchTerm = null, int? categoryId = null, string? priorityFilter = null, DateTime? dateFrom = null, DateTime? dateTo = null, int page = 1, int pageSize = 10, int? exportJobId = null)
        {
            var allowedPageSizes = new[] { 10, 20, 50 };
            if (!allowedPageSizes.Contains(pageSize))
            {
                pageSize = 10;
            }

            if (page < 1)
            {
                page = 1;
            }

            var query = _dbContext.Tickets
                .AsNoTracking()
                .Where(t => (t.Status == TicketStatus.Resolved || t.Status == TicketStatus.Closed || t.Status == TicketStatus.Cancelled)
                         && t.ServiceFinishedAtUtc != null);

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var normalizedSearch = searchTerm.Trim();
                query = query.Where(t =>
                    (t.Student != null && (
                        (t.Student.FullName != null && t.Student.FullName.Contains(normalizedSearch))
                        || (t.Student.Email != null && t.Student.Email.Contains(normalizedSearch))))
                    || (t.AssignedProfessor != null && (
                        (t.AssignedProfessor.FullName != null && t.AssignedProfessor.FullName.Contains(normalizedSearch))
                        || (t.AssignedProfessor.Email != null && t.AssignedProfessor.Email.Contains(normalizedSearch)))
                    )
                    || t.TicketNumber.Contains(normalizedSearch));
            }

            if (categoryId.HasValue)
            {
                query = query.Where(t => t.CategoryId == categoryId.Value);
            }

            if (!string.IsNullOrWhiteSpace(priorityFilter)
                && Enum.TryParse<TicketPriority>(priorityFilter, true, out var parsedPriority))
            {
                query = query.Where(t => t.Priority == parsedPriority);
            }

            if (dateFrom.HasValue)
            {
                var fromUtc = DateTime.SpecifyKind(dateFrom.Value.Date, DateTimeKind.Local).ToUniversalTime();
                query = query.Where(t => t.ServiceFinishedAtUtc >= fromUtc);
            }

            if (dateTo.HasValue)
            {
                var endLocal = dateTo.Value.Date.AddDays(1).AddTicks(-1);
                var endUtc = DateTime.SpecifyKind(endLocal, DateTimeKind.Local).ToUniversalTime();
                query = query.Where(t => t.ServiceFinishedAtUtc <= endUtc);
            }

            var totalRecords = await query.CountAsync();
            var totalPages = Math.Max(1, (int)Math.Ceiling(totalRecords / (double)pageSize));
            if (page > totalPages)
            {
                page = totalPages;
            }

            var items = await query
                .OrderByDescending(t => t.ServiceFinishedAtUtc)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(t => new TicketAttendanceHistoryItemViewModel
                {
                    Id = t.Id,
                    TicketNumber = t.TicketNumber,
                    StudentName = t.Student != null ? (t.Student.FullName ?? t.Student.Email ?? "Aluno") : "Aluno",
                    StudentEmail = t.Student != null ? (t.Student.Email ?? "-") : "-",
                    ProfessorName = t.AssignedProfessor != null ? (t.AssignedProfessor.FullName ?? t.AssignedProfessor.Email ?? "Docente") : "Docente não informado",
                    ProfessorEmail = t.AssignedProfessor != null ? (t.AssignedProfessor.Email ?? "-") : "-",
                    CategoryName = t.Category != null ? t.Category.Name : "-",
                    Status = t.Status,
                    Priority = t.Priority,
                    ServiceFinishedAtUtc = t.ServiceFinishedAtUtc!.Value,
                    TotalServiceMinutes = t.ServiceStartedAtUtc != null
                        ? EF.Functions.DateDiffMinute(t.ServiceStartedAtUtc.Value, t.ServiceFinishedAtUtc!.Value)
                        : EF.Functions.DateDiffMinute(t.CreatedAtUtc, t.ServiceFinishedAtUtc!.Value)
                })
                .ToListAsync();

            var model = new TicketAttendanceHistoryPageViewModel
            {
                Items = items,
                SearchTerm = searchTerm?.Trim() ?? string.Empty,
                CategoryId = categoryId,
                PriorityFilter = priorityFilter?.Trim() ?? string.Empty,
                DateFrom = dateFrom,
                DateTo = dateTo,
                ExportJobId = exportJobId,
                CategoryOptions = await BuildCategoryFilterOptionsAsync(categoryId),
                PriorityOptions = BuildPriorityFilterOptions(priorityFilter),
                PageSize = pageSize,
                CurrentPage = page,
                TotalPages = totalPages,
                TotalRecords = totalRecords
            };

            return View(model);
        }

        // Registra solicitação assíncrona de exportação em PDF com filtros atuais da listagem.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Professor,Admin")]
        public async Task<IActionResult> RequestHistoryPdf(string? searchTerm = null, int? categoryId = null, string? priorityFilter = null, DateTime? dateFrom = null, DateTime? dateTo = null, int pageSize = 10)
        {
            var requester = await _userManager.GetUserAsync(User);
            if (requester is null)
            {
                return Challenge();
            }

            DateTime? fromUtc = dateFrom.HasValue
                ? DateTime.SpecifyKind(dateFrom.Value.Date, DateTimeKind.Local).ToUniversalTime()
                : null;
            DateTime? toUtc = dateTo.HasValue
                ? DateTime.SpecifyKind(dateTo.Value.Date.AddDays(1).AddTicks(-1), DateTimeKind.Local).ToUniversalTime()
                : null;

            var job = new AttendanceReportExportJob
            {
                RequesterId = requester.Id,
                Status = AttendanceReportExportStatus.Pending,
                SearchTerm = string.IsNullOrWhiteSpace(searchTerm) ? null : searchTerm.Trim(),
                CategoryId = categoryId,
                PriorityFilter = string.IsNullOrWhiteSpace(priorityFilter) ? null : priorityFilter.Trim(),
                DateFromUtc = fromUtc,
                DateToUtc = toUtc,
                CreatedAtUtc = DateTime.UtcNow
            };

            _dbContext.AttendanceReportExportJobs.Add(job);
            await _dbContext.SaveChangesAsync();

            TempData["TicketSuccess"] = "Exportação em PDF iniciada em segundo plano. Você será notificado quando estiver pronta.";

            return RedirectToAction(nameof(History), new
            {
                searchTerm,
                categoryId,
                priorityFilter,
                dateFrom,
                dateTo,
                page = 1,
                pageSize,
                exportJobId = job.Id
            });
        }

        // Retorna status de processamento do job de exportação solicitado.
        [HttpGet]
        [Authorize(Roles = "Professor,Admin")]
        public async Task<IActionResult> HistoryExportStatus(int jobId)
        {
            var requester = await _userManager.GetUserAsync(User);
            if (requester is null)
            {
                return Challenge();
            }

            var isAdmin = User.IsInRole("Admin");

            var job = await _dbContext.AttendanceReportExportJobs
                .AsNoTracking()
                .FirstOrDefaultAsync(j => j.Id == jobId);

            if (job is null)
            {
                return NotFound();
            }

            if (!isAdmin && job.RequesterId != requester.Id)
            {
                return Forbid();
            }

            var downloadUrl = job.Status == AttendanceReportExportStatus.Completed
                ? Url.Action(nameof(DownloadHistoryPdf), new { jobId })
                : null;

            return Json(new
            {
                status = job.Status.ToString(),
                errorMessage = job.ErrorMessage,
                downloadUrl
            });
        }

        // Realiza download do arquivo PDF já processado do histórico.
        [HttpGet]
        [Authorize(Roles = "Professor,Admin")]
        public async Task<IActionResult> DownloadHistoryPdf(int jobId)
        {
            var requester = await _userManager.GetUserAsync(User);
            if (requester is null)
            {
                return Challenge();
            }

            var isAdmin = User.IsInRole("Admin");

            var job = await _dbContext.AttendanceReportExportJobs
                .AsNoTracking()
                .FirstOrDefaultAsync(j => j.Id == jobId);

            if (job is null)
            {
                return NotFound();
            }

            if (!isAdmin && job.RequesterId != requester.Id)
            {
                return Forbid();
            }

            if (job.Status != AttendanceReportExportStatus.Completed || string.IsNullOrWhiteSpace(job.RelativePath))
            {
                TempData["TicketError"] = "O relatório ainda não está disponível para download.";
                return RedirectToAction(nameof(History));
            }

            var fullPath = Path.Combine(_environment.ContentRootPath, "App_Data", "reports", job.RelativePath);
            if (!System.IO.File.Exists(fullPath))
            {
                TempData["TicketError"] = "Arquivo do relatório não encontrado.";
                return RedirectToAction(nameof(History));
            }

            var bytes = await System.IO.File.ReadAllBytesAsync(fullPath);
            return File(bytes, "application/pdf", job.OutputFileName ?? $"historico-atendimentos-{jobId}.pdf");
        }

        // Direciona o professor para o próximo chamado da fila e anuncia o atendimento.
        [Authorize(Roles = "Professor,Admin")]
        public async Task<IActionResult> AttendNext()
        {
            var currentProfessor = await _userManager.GetUserAsync(User);
            if (currentProfessor is null)
            {
                return Challenge();
            }

            var nowUtc = DateTime.UtcNow;
            const int priorityBoostMinutes = 240;
            var isAdmin = User.IsInRole("Admin");

            var nextTicket = await _dbContext.Tickets
                .Where(t => t.Status == TicketStatus.Open)
                .Where(t => isAdmin || t.PreferredProfessorId == null || t.PreferredProfessorId == currentProfessor.Id)
                .Select(t => new
                {
                    Ticket = t,
                    StudentName = t.Student != null
                        ? (t.Student.FullName ?? t.Student.Email ?? "Aluno")
                        : "Aluno",
                    QueueScore = EF.Functions.DateDiffMinute(t.CreatedAtUtc, nowUtc) + ((int)t.Priority * priorityBoostMinutes)
                })
                .OrderByDescending(t => t.QueueScore)
                .ThenBy(t => t.Ticket.CreatedAtUtc)
                .FirstOrDefaultAsync();

            if (nextTicket is null)
            {
                TempData["TicketError"] = "Não há chamados pendentes disponíveis para o seu atendimento.";
                return RedirectToAction(nameof(Queue));
            }

            StartAttendance(nextTicket.Ticket, currentProfessor.Id, nowUtc);
            await SaveCallAnnouncementAsync(nextTicket.Ticket.TicketNumber, nextTicket.StudentName, nowUtc);

            TempData["TicketSuccess"] = $"Atendimento iniciado para o chamado {nextTicket.Ticket.TicketNumber}.";

            return RedirectToAction(nameof(Details), new { id = nextTicket.Ticket.Id });
        }

        // Permite iniciar atendimento direto de um ticket específico da fila (fura-fila).
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Professor,Admin")]
        public async Task<IActionResult> AttendNow(int id)
        {
            var currentProfessor = await _userManager.GetUserAsync(User);
            if (currentProfessor is null)
            {
                return Challenge();
            }

            var nowUtc = DateTime.UtcNow;
            var isAdmin = User.IsInRole("Admin");

            var ticket = await _dbContext.Tickets
                .Where(t => t.Id == id)
                .Select(t => new
                {
                    Ticket = t,
                    StudentName = t.Student != null
                        ? (t.Student.FullName ?? t.Student.Email ?? "Aluno")
                        : "Aluno"
                })
                .FirstOrDefaultAsync();

            if (ticket is null)
            {
                TempData["TicketError"] = "Chamado não encontrado para atendimento direto.";
                return RedirectToAction(nameof(Queue));
            }

            if (ticket.Ticket.Status is TicketStatus.Closed or TicketStatus.Cancelled)
            {
                TempData["TicketError"] = "Chamado fechado/cancelado não pode ser atendido.";
                return RedirectToAction(nameof(Queue));
            }

            if (!isAdmin && !string.IsNullOrWhiteSpace(ticket.Ticket.PreferredProfessorId) && ticket.Ticket.PreferredProfessorId != currentProfessor.Id)
            {
                TempData["TicketError"] = "Este chamado está reservado para outro professor.";
                return RedirectToAction(nameof(Queue));
            }

            StartAttendance(ticket.Ticket, currentProfessor.Id, nowUtc);
            await SaveCallAnnouncementAsync(ticket.Ticket.TicketNumber, ticket.StudentName, nowUtc);

            TempData["TicketSuccess"] = $"Atendimento iniciado para o chamado {ticket.Ticket.TicketNumber}.";

            return RedirectToAction(nameof(Details), new { id = ticket.Ticket.Id });
        }

        // Constrói opções de professores para seleção opcional na abertura.
        private async Task<IReadOnlyCollection<SelectListItem>> BuildProfessorOptionsAsync()
        {
            var professors = await _userManager.GetUsersInRoleAsync("Professor");

            return professors
                .OrderBy(p => p.FullName ?? p.Email)
                .Select(p => new SelectListItem
                {
                    Value = p.Id,
                    Text = p.FullName ?? p.Email ?? "Professor"
                })
                .ToList();
        }

        // Exibe detalhes do chamado com anexos e histórico de comentários.
        public async Task<IActionResult> Details(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user is null)
            {
                return Challenge();
            }

            var ticket = await _dbContext.Tickets
                .AsNoTracking()
                .Where(t => t.Id == id)
                .Select(t => new
                {
                    Ticket = t,
                    StudentName = t.Student != null ? (t.Student.FullName ?? t.Student.Email ?? "Aluno") : "Aluno"
                })
                .FirstOrDefaultAsync();

            if (ticket is null)
            {
                TempData["TicketError"] = "Chamado não encontrado.";
                return RedirectToAction(nameof(My));
            }

            var isStaff = User.IsInRole("Professor") || User.IsInRole("Admin");
            if (!isStaff && ticket.Ticket.StudentId != user.Id)
            {
                return Forbid();
            }

            var comments = await _dbContext.TicketComments
                .AsNoTracking()
                .Where(c => c.TicketId == ticket.Ticket.Id)
                .OrderBy(c => c.CreatedAtUtc)
                .Select(c => new TicketCommentViewModel
                {
                    AuthorName = c.Author != null ? (c.Author.FullName ?? c.Author.Email ?? "Usuário") : "Usuário",
                    Message = c.Message,
                    CreatedAtUtc = c.CreatedAtUtc
                })
                .ToListAsync();

            var model = new TicketDetailsViewModel
            {
                Id = ticket.Ticket.Id,
                TicketNumber = ticket.Ticket.TicketNumber,
                CategoryName = ticket.Ticket.Category != null ? ticket.Ticket.Category.Name : "-",
                Priority = ticket.Ticket.Priority,
                Status = ticket.Ticket.Status,
                StudentName = ticket.StudentName,
                PreferredProfessorName = ticket.Ticket.PreferredProfessor != null
                    ? (ticket.Ticket.PreferredProfessor.FullName ?? ticket.Ticket.PreferredProfessor.Email)
                    : null,
                Description = ticket.Ticket.Description,
                CreatedAtUtc = ticket.Ticket.CreatedAtUtc,
                ResponseDueAtUtc = ticket.Ticket.ResponseDueAtUtc,
                IsOverdue = (ticket.Ticket.Status is TicketStatus.Open or TicketStatus.InProgress)
                            && ticket.Ticket.ResponseDueAtUtc < DateTime.UtcNow,
                OverdueMinutes = (ticket.Ticket.Status is TicketStatus.Open or TicketStatus.InProgress)
                                ? Math.Max(0, (int)(DateTime.UtcNow - ticket.Ticket.ResponseDueAtUtc).TotalMinutes)
                                : 0,
                Attachments = ticket.Ticket.Attachments
                    .OrderBy(a => a.Id)
                    .ToList(),
                Comments = comments,
                CanChangeStatus = isStaff
            };

            return View(model);
        }

        // Calcula prazo de primeira resposta com base na prioridade do chamado.
        private static DateTime CalculateResponseDueAtUtc(DateTime openedAtUtc, TicketPriority priority)
        {
            var targetWindow = priority switch
            {
                TicketPriority.Urgent => TimeSpan.FromHours(2),
                TicketPriority.High => TimeSpan.FromHours(8),
                TicketPriority.Medium => TimeSpan.FromHours(24),
                _ => TimeSpan.FromHours(72)
            };

            return openedAtUtc.Add(targetWindow);
        }

        // Adiciona comentário no histórico do chamado.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddComment(int id, [Bind(Prefix = "CommentInput")] TicketCommentInputModel input)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user is null)
            {
                return Challenge();
            }

            var ticket = await _dbContext.Tickets
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == id);

            if (ticket is null)
            {
                TempData["TicketError"] = "Chamado não encontrado.";
                return RedirectToAction(nameof(My));
            }

            var isStaff = User.IsInRole("Professor") || User.IsInRole("Admin");
            if (!isStaff && ticket.StudentId != user.Id)
            {
                return Forbid();
            }

            if (!ModelState.IsValid)
            {
                TempData["TicketError"] = "Não foi possível enviar o comentário. Verifique a mensagem.";
                return RedirectToAction(nameof(Details), new { id });
            }

            _dbContext.TicketComments.Add(new TicketComment
            {
                TicketId = id,
                AuthorId = user.Id,
                Message = input.Message.Trim(),
                CreatedAtUtc = DateTime.UtcNow
            });

            await _dbContext.SaveChangesAsync();

            TempData["TicketSuccess"] = "Comentário adicionado com sucesso.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // Atualiza status do chamado no fluxo de atendimento.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Professor,Admin")]
        public async Task<IActionResult> UpdateStatus(int id, TicketStatus status)
        {
            var ticket = await _dbContext.Tickets.FirstOrDefaultAsync(t => t.Id == id);
            if (ticket is null)
            {
                TempData["TicketError"] = "Chamado não encontrado.";
                return RedirectToAction(nameof(Queue));
            }

            var nowUtc = DateTime.UtcNow;
            if (status == TicketStatus.InProgress)
            {
                var currentProfessor = await _userManager.GetUserAsync(User);
                if (currentProfessor is null)
                {
                    return Challenge();
                }

                StartAttendance(ticket, currentProfessor.Id, nowUtc);
            }

            if (status is TicketStatus.Resolved or TicketStatus.Closed)
            {
                if (string.IsNullOrWhiteSpace(ticket.AssignedProfessorId))
                {
                    var currentProfessor = await _userManager.GetUserAsync(User);
                    if (currentProfessor is not null)
                    {
                        ticket.AssignedProfessorId = currentProfessor.Id;
                    }
                }

                ticket.ServiceStartedAtUtc ??= nowUtc;
                ticket.ServiceFinishedAtUtc = nowUtc;
            }

            if (ticket.Status == TicketStatus.Closed && status == TicketStatus.Cancelled)
            {
                TempData["TicketError"] = "Chamado fechado não pode ser alterado para cancelado.";
                return RedirectToAction(nameof(Details), new { id });
            }

            if (status == TicketStatus.Open)
            {
                ticket.AssignedProfessorId = null;
                ticket.ServiceStartedAtUtc = null;
                ticket.ServiceFinishedAtUtc = null;
            }

            if (status == TicketStatus.Cancelled)
            {
                ticket.AssignedProfessorId = null;
                ticket.ServiceStartedAtUtc = null;
                ticket.ServiceFinishedAtUtc = nowUtc;
            }

            ticket.Status = status;
            await _dbContext.SaveChangesAsync();

            TempData["TicketSuccess"] = $"Status do chamado {ticket.TicketNumber} atualizado.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // Devolve chamado para a fila quando não foi resolvido no atendimento atual.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Professor,Admin")]
        public async Task<IActionResult> ReturnToQueue(int id)
        {
            var ticket = await _dbContext.Tickets.FirstOrDefaultAsync(t => t.Id == id);
            if (ticket is null)
            {
                TempData["TicketError"] = "Chamado não encontrado.";
                return RedirectToAction(nameof(Queue));
            }

            if (ticket.Status == TicketStatus.Closed)
            {
                TempData["TicketError"] = "Chamado fechado não pode voltar para a fila.";
                return RedirectToAction(nameof(Details), new { id });
            }

            if (ticket.Status == TicketStatus.Cancelled)
            {
                TempData["TicketError"] = "Chamado cancelado não pode voltar para a fila.";
                return RedirectToAction(nameof(Details), new { id });
            }

            ticket.Status = TicketStatus.Open;
            ticket.AssignedProfessorId = null;
            ticket.ServiceStartedAtUtc = null;
            ticket.ServiceFinishedAtUtc = null;
            await _dbContext.SaveChangesAsync();

            TempData["TicketSuccess"] = $"Chamado {ticket.TicketNumber} devolvido para a fila.";
            return RedirectToAction(nameof(Queue));
        }

        // Cancela chamado na listagem, disponível para aluno dono e equipe de atendimento.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Aluno,Professor,Admin")]
        public async Task<IActionResult> CancelTicket(int id, string? returnTo = null)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user is null)
            {
                return Challenge();
            }

            var ticket = await _dbContext.Tickets.FirstOrDefaultAsync(t => t.Id == id);
            if (ticket is null)
            {
                TempData["TicketError"] = "Chamado não encontrado.";
                return RedirectToAction(nameof(My));
            }

            var isStaff = User.IsInRole("Professor") || User.IsInRole("Admin");
            if (!isStaff && ticket.StudentId != user.Id)
            {
                return Forbid();
            }

            if (ticket.Status is TicketStatus.Resolved or TicketStatus.Closed or TicketStatus.Cancelled)
            {
                TempData["TicketError"] = "Somente chamados em aberto/em atendimento podem ser cancelados.";
                return RedirectToHistoryTarget(returnTo, ticket.Id, isStaff);
            }

            ticket.Status = TicketStatus.Cancelled;
            ticket.AssignedProfessorId = null;
            ticket.ServiceStartedAtUtc = null;
            ticket.ServiceFinishedAtUtc = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync();

            TempData["TicketSuccess"] = $"Chamado {ticket.TicketNumber} cancelado com sucesso.";
            return RedirectToHistoryTarget(returnTo, ticket.Id, isStaff);
        }

        // Resolve redirecionamento pós-ação com base no contexto de origem da listagem.
        private IActionResult RedirectToHistoryTarget(string? returnTo, int ticketId, bool isStaff)
        {
            return (returnTo ?? string.Empty).ToLowerInvariant() switch
            {
                "queue" => RedirectToAction(nameof(Queue)),
                "details" => RedirectToAction(nameof(Details), new { id = ticketId }),
                _ => isStaff ? RedirectToAction(nameof(Queue)) : RedirectToAction(nameof(My))
            };
        }

        // Gera o identificador sequencial mensal no padrão AAAA-MM-000001.
        private async Task<string> GenerateTicketNumberAsync(DateTime openedAtUtc)
        {
            var prefix = openedAtUtc.ToString("yyyy-MM");
            var nextSequence = await _dbContext.Tickets
                .CountAsync(t => t.TicketNumber.StartsWith(prefix + "-")) + 1;

            var candidate = $"{prefix}-{nextSequence:D6}";

            while (await _dbContext.Tickets.AnyAsync(t => t.TicketNumber == candidate))
            {
                nextSequence++;
                candidate = $"{prefix}-{nextSequence:D6}";
            }

            return candidate;
        }

        // Persiste arquivos enviados no disco e registra metadados no banco.
        private async Task SaveAttachmentsAsync(Ticket ticket, IReadOnlyCollection<IFormFile> files)
        {
            if (files.Count == 0)
            {
                return;
            }

            var rootPath = _environment.WebRootPath;
            var uploadDirectory = Path.Combine(rootPath, "uploads", "tickets", ticket.CreatedAtUtc.ToString("yyyy"), ticket.CreatedAtUtc.ToString("MM"), ticket.Id.ToString());
            Directory.CreateDirectory(uploadDirectory);

            foreach (var file in files)
            {
                if (file.Length <= 0)
                {
                    continue;
                }

                var extension = Path.GetExtension(file.FileName);
                var generatedFileName = $"{Guid.NewGuid():N}{extension}";
                var fullPath = Path.Combine(uploadDirectory, generatedFileName);

                await using var stream = System.IO.File.Create(fullPath);
                await file.CopyToAsync(stream);

                var relativePath = Path.Combine("uploads", "tickets", ticket.CreatedAtUtc.ToString("yyyy"), ticket.CreatedAtUtc.ToString("MM"), ticket.Id.ToString(), generatedFileName)
                    .Replace("\\", "/");

                _dbContext.TicketAttachments.Add(new TicketAttachment
                {
                    TicketId = ticket.Id,
                    OriginalFileName = file.FileName,
                    RelativePath = "/" + relativePath,
                    ContentType = file.ContentType,
                    SizeBytes = file.Length
                });
            }
        }

        // Constrói as opções de categoria disponíveis no formulário.
        private async Task<IReadOnlyCollection<SelectListItem>> BuildCategoryOptionsAsync()
        {
            return await _dbContext.Categories
                .AsNoTracking()
                .OrderBy(c => c.Name)
                .Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = c.Name
                })
                .ToListAsync();
        }

        // Constrói opções de categoria para filtros de histórico.
        private async Task<IReadOnlyCollection<SelectListItem>> BuildCategoryFilterOptionsAsync(int? selectedCategoryId)
        {
            var options = await _dbContext.Categories
                .AsNoTracking()
                .OrderBy(c => c.Name)
                .Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = c.Name,
                    Selected = selectedCategoryId.HasValue && c.Id == selectedCategoryId.Value
                })
                .ToListAsync();

            options.Insert(0, new SelectListItem
            {
                Value = string.Empty,
                Text = "Todas",
                Selected = !selectedCategoryId.HasValue
            });

            return options;
        }

        // Constrói opções de prioridade para o select da interface.
        private static IReadOnlyCollection<SelectListItem> BuildPriorityOptions()
        {
            return new List<SelectListItem>
            {
                new() { Value = TicketPriority.Low.ToString(), Text = "Baixa" },
                new() { Value = TicketPriority.Medium.ToString(), Text = "Média" },
                new() { Value = TicketPriority.High.ToString(), Text = "Alta" },
                new() { Value = TicketPriority.Urgent.ToString(), Text = "Urgente" }
            };
        }

        // Constrói opções de prioridade para filtros de histórico.
        private static IReadOnlyCollection<SelectListItem> BuildPriorityFilterOptions(string? selectedPriority)
        {
            var normalized = selectedPriority?.Trim() ?? string.Empty;

            return new List<SelectListItem>
            {
                new() { Value = string.Empty, Text = "Todas", Selected = string.IsNullOrWhiteSpace(normalized) },
                new() { Value = TicketPriority.Low.ToString(), Text = "Baixa", Selected = normalized == TicketPriority.Low.ToString() },
                new() { Value = TicketPriority.Medium.ToString(), Text = "Média", Selected = normalized == TicketPriority.Medium.ToString() },
                new() { Value = TicketPriority.High.ToString(), Text = "Alta", Selected = normalized == TicketPriority.High.ToString() },
                new() { Value = TicketPriority.Urgent.ToString(), Text = "Urgente", Selected = normalized == TicketPriority.Urgent.ToString() }
            };
        }

        // Marca início de atendimento do ticket para o professor atual.
        private static void StartAttendance(Ticket ticket, string professorId, DateTime nowUtc)
        {
            ticket.Status = TicketStatus.InProgress;
            ticket.AssignedProfessorId = professorId;
            ticket.ServiceStartedAtUtc = nowUtc;
            ticket.ServiceFinishedAtUtc = null;
        }

        // Persiste anúncio de chamada para exibição no painel público da TV.
        private async Task SaveCallAnnouncementAsync(string ticketNumber, string studentName, DateTime nowUtc)
        {
            _dbContext.TicketCallAnnouncements.Add(new TicketCallAnnouncement
            {
                TicketNumber = ticketNumber,
                StudentName = studentName,
                CalledAtUtc = nowUtc
            });

            await _dbContext.SaveChangesAsync();
        }

        // Converte texto de links em coleção válida de URLs absolutas.
        private static List<string>? ParseLinks(string? rawLinks)
        {
            if (string.IsNullOrWhiteSpace(rawLinks))
            {
                return [];
            }

            var links = rawLinks
                .Split(['\r', '\n', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            foreach (var link in links)
            {
                if (!Uri.TryCreate(link, UriKind.Absolute, out var uri)
                    || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
                {
                    return null;
                }
            }

            return links;
        }
    }
}
