using DevHelp.Data;
using DevHelp.Models;
using DevHelp.Models.Identity;
using DevHelp.Models.Tickets;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace DevHelp.Controllers
{
    public class HomeController(
        ApplicationDbContext dbContext,
        UserManager<ApplicationUser> userManager) : Controller
    {
        // Acesso ao banco para leitura de dados operacionais do dashboard.
        private readonly ApplicationDbContext _dbContext = dbContext;
        // Acesso aos usuários por papel para montar visão de professores.
        private readonly UserManager<ApplicationUser> _userManager = userManager;

        // Mantém a home operacional acessível apenas para usuários autenticados.
        [Authorize]
        public async Task<IActionResult> Index()
        {
            var model = await BuildDashboardModelAsync();
            return View(model);
        }

        // Exibe versão pública do dashboard para monitor em TV.
        [AllowAnonymous]
        [HttpGet("/painel-tv")]
        public async Task<IActionResult> PublicDashboard()
        {
            var model = await BuildDashboardModelAsync();
            return View(model);
        }

        // Retorna a última chamada de ticket para painel público em tempo real.
        [AllowAnonymous]
        [HttpGet("/painel-tv/ultima-chamada")]
        public async Task<IActionResult> GetLatestCallAnnouncement()
        {
            var latestCall = await _dbContext.TicketCallAnnouncements
                .AsNoTracking()
                .OrderByDescending(x => x.Id)
                .Select(x => new
                {
                    x.Id,
                    x.TicketNumber,
                    x.StudentName,
                    x.CalledAtUtc
                })
                .FirstOrDefaultAsync();

            return Json(latestCall);
        }

        // Monta todos os dados analíticos e operacionais usados nos dashboards.
        private async Task<HomeDashboardViewModel> BuildDashboardModelAsync()
        {
            var nowUtc = DateTime.UtcNow;
            const int priorityBoostMinutes = 240;

            var professors = await _userManager.GetUsersInRoleAsync("Professor");
            var currentAttendances = await _dbContext.Tickets
                .AsNoTracking()
                .Where(t => t.Status == TicketStatus.InProgress && t.AssignedProfessorId != null)
                .Select(t => new
                {
                    t.AssignedProfessorId,
                    t.TicketNumber,
                    t.ServiceStartedAtUtc
                })
                .ToListAsync();

            var currentByProfessor = currentAttendances
                .Where(a => a.AssignedProfessorId != null)
                .GroupBy(a => a.AssignedProfessorId!)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.ServiceStartedAtUtc).First());

            var professorCards = professors
                .OrderBy(p => p.FullName ?? p.Email)
                .Select(p =>
                {
                    currentByProfessor.TryGetValue(p.Id, out var attendance);

                    return new ProfessorCurrentAttendanceViewModel
                    {
                        ProfessorName = p.FullName ?? p.Email ?? "Professor",
                        TicketNumber = attendance?.TicketNumber,
                        ServiceStartedAtUtc = attendance?.ServiceStartedAtUtc
                    };
                })
                .ToList();

            var nextQueue = await _dbContext.Tickets
                .AsNoTracking()
                .Where(t => t.Status == TicketStatus.Open)
                .Select(t => new DashboardQueueItemViewModel
                {
                    TicketNumber = t.TicketNumber,
                    StudentName = t.Student != null
                        ? (t.Student.FullName ?? t.Student.Email ?? "Aluno")
                        : "Aluno",
                    Priority = t.Priority.ToString(),
                    QueueScore = EF.Functions.DateDiffMinute(t.CreatedAtUtc, nowUtc) + ((int)t.Priority * priorityBoostMinutes)
                })
                .OrderByDescending(t => t.QueueScore)
                .Take(8)
                .ToListAsync();

            var latestFinished = await _dbContext.Tickets
                .AsNoTracking()
                .Where(t => (t.Status == TicketStatus.Resolved || t.Status == TicketStatus.Closed)
                         && t.ServiceFinishedAtUtc != null)
                .OrderByDescending(t => t.ServiceFinishedAtUtc)
                .Take(12)
                .Select(t => new FinishedAttendanceViewModel
                {
                    ProfessorName = t.AssignedProfessor != null
                        ? (t.AssignedProfessor.FullName ?? t.AssignedProfessor.Email ?? "Professor")
                        : "Professor não informado",
                    TicketNumber = t.TicketNumber,
                    ServiceFinishedAtUtc = t.ServiceFinishedAtUtc!.Value,
                    TotalServiceMinutes = t.ServiceStartedAtUtc != null
                        ? EF.Functions.DateDiffMinute(t.ServiceStartedAtUtc.Value, t.ServiceFinishedAtUtc!.Value)
                        : EF.Functions.DateDiffMinute(t.CreatedAtUtc, t.ServiceFinishedAtUtc!.Value)
                })
                .ToListAsync();

            var categoryDistribution = await _dbContext.Tickets
                .AsNoTracking()
                .Where(t => t.Status == TicketStatus.Open || t.Status == TicketStatus.InProgress)
                .GroupBy(t => t.Category != null ? t.Category.Name : "Sem categoria")
                .Select(g => new DashboardChartItemViewModel
                {
                    Label = g.Key,
                    Value = g.Count()
                })
                .OrderByDescending(x => x.Value)
                .Take(8)
                .ToListAsync();

            var priorityDistributionRaw = await _dbContext.Tickets
                .AsNoTracking()
                .Where(t => t.Status == TicketStatus.Open || t.Status == TicketStatus.InProgress)
                .GroupBy(t => t.Priority)
                .Select(g => new
                {
                    Priority = g.Key,
                    Count = g.Count()
                })
                .OrderByDescending(x => x.Priority)
                .ToListAsync();

            static string PriorityLabel(TicketPriority priority)
            {
                return priority switch
                {
                    TicketPriority.Urgent => "Urgente",
                    TicketPriority.High => "Alta",
                    TicketPriority.Medium => "Média",
                    _ => "Baixa"
                };
            }

            var priorityDistribution = priorityDistributionRaw
                .Select(x => new DashboardChartItemViewModel
                {
                    Label = PriorityLabel(x.Priority),
                    Value = x.Count
                })
                .ToList();

            var pendingOverdueCount = await _dbContext.Tickets
                .AsNoTracking()
                .CountAsync(t => (t.Status == TicketStatus.Open || t.Status == TicketStatus.InProgress)
                              && t.ResponseDueAtUtc < nowUtc);

            var pendingOnTimeCount = await _dbContext.Tickets
                .AsNoTracking()
                .CountAsync(t => (t.Status == TicketStatus.Open || t.Status == TicketStatus.InProgress)
                              && t.ResponseDueAtUtc >= nowUtc);

            var finishedWithinSlaCount = await _dbContext.Tickets
                .AsNoTracking()
                .CountAsync(t => (t.Status == TicketStatus.Resolved || t.Status == TicketStatus.Closed)
                              && t.ServiceFinishedAtUtc != null
                              && t.ServiceFinishedAtUtc <= t.ResponseDueAtUtc);

            var finishedOutOfSlaCount = await _dbContext.Tickets
                .AsNoTracking()
                .CountAsync(t => (t.Status == TicketStatus.Resolved || t.Status == TicketStatus.Closed)
                              && t.ServiceFinishedAtUtc != null
                              && t.ServiceFinishedAtUtc > t.ResponseDueAtUtc);

            var finishedByProfessor = await _dbContext.Tickets
                .AsNoTracking()
                .Where(t => (t.Status == TicketStatus.Resolved || t.Status == TicketStatus.Closed)
                         && t.ServiceFinishedAtUtc != null)
                .GroupBy(t => t.AssignedProfessor != null
                    ? (t.AssignedProfessor.FullName ?? t.AssignedProfessor.Email ?? "Professor")
                    : "Professor não informado")
                .Select(g => new DashboardChartItemViewModel
                {
                    Label = g.Key,
                    Value = g.Count()
                })
                .OrderByDescending(x => x.Value)
                .Take(8)
                .ToListAsync();

            var dailyRangeStart = nowUtc.Date.AddDays(-9);
            var dailyTickets = await _dbContext.Tickets
                .AsNoTracking()
                .Where(t => t.CreatedAtUtc >= dailyRangeStart || (t.ServiceFinishedAtUtc != null && t.ServiceFinishedAtUtc >= dailyRangeStart))
                .Select(t => new
                {
                    t.CreatedAtUtc,
                    t.ServiceFinishedAtUtc
                })
                .ToListAsync();

            var dailyFlow = Enumerable.Range(0, 10)
                .Select(offset =>
                {
                    var day = dailyRangeStart.AddDays(offset);
                    var nextDay = day.AddDays(1);
                    return new DashboardDailyFlowViewModel
                    {
                        DayUtc = day,
                        OpenedCount = dailyTickets.Count(t => t.CreatedAtUtc >= day && t.CreatedAtUtc < nextDay),
                        FinishedCount = dailyTickets.Count(t => t.ServiceFinishedAtUtc != null && t.ServiceFinishedAtUtc >= day && t.ServiceFinishedAtUtc < nextDay)
                    };
                })
                .ToList();

            return new HomeDashboardViewModel
            {
                ProfessorsCount = professors.Count,
                InProgressCount = currentAttendances.Count,
                QueueCount = await _dbContext.Tickets.CountAsync(t => t.Status == TicketStatus.Open),
                ProfessorsCurrentAttendance = professorCards,
                NextQueue = nextQueue,
                LatestFinishedAttendances = latestFinished,
                CategoryDistribution = categoryDistribution,
                PriorityDistribution = priorityDistribution,
                SlaOverview = new DashboardSlaOverviewViewModel
                {
                    PendingOverdueCount = pendingOverdueCount,
                    PendingOnTimeCount = pendingOnTimeCount,
                    FinishedWithinSlaCount = finishedWithinSlaCount,
                    FinishedOutOfSlaCount = finishedOutOfSlaCount
                },
                FinishedByProfessor = finishedByProfessor,
                DailyFlow = dailyFlow
            };
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
