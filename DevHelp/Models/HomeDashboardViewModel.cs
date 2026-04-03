namespace DevHelp.Models
{
    // Modelo consolidado para exibição do dashboard inicial operacional.
    public class HomeDashboardViewModel
    {
        // Total de professores ativos no sistema.
        public int ProfessorsCount { get; set; }

        // Quantidade de chamados atualmente em atendimento.
        public int InProgressCount { get; set; }

        // Quantidade de chamados pendentes na fila.
        public int QueueCount { get; set; }

        // Professores com seu atendimento atual (quando houver).
        public IReadOnlyCollection<ProfessorCurrentAttendanceViewModel> ProfessorsCurrentAttendance { get; set; } = [];

        // Próximos chamados da fila por ordem de atendimento.
        public IReadOnlyCollection<DashboardQueueItemViewModel> NextQueue { get; set; } = [];

        // Histórico dos últimos atendimentos finalizados.
        public IReadOnlyCollection<FinishedAttendanceViewModel> LatestFinishedAttendances { get; set; } = [];

        // Distribuição de chamados por categoria.
        public IReadOnlyCollection<DashboardChartItemViewModel> CategoryDistribution { get; set; } = [];

        // Distribuição de chamados por prioridade.
        public IReadOnlyCollection<DashboardChartItemViewModel> PriorityDistribution { get; set; } = [];

        // Indicadores consolidados de SLA.
        public DashboardSlaOverviewViewModel SlaOverview { get; set; } = new();

        // Quantidade de atendimentos finalizados por professor.
        public IReadOnlyCollection<DashboardChartItemViewModel> FinishedByProfessor { get; set; } = [];

        // Série temporal de aberturas e finalizações recentes.
        public IReadOnlyCollection<DashboardDailyFlowViewModel> DailyFlow { get; set; } = [];
    }

    // Representa estado atual de atendimento por professor.
    public class ProfessorCurrentAttendanceViewModel
    {
        // Nome exibido do professor.
        public string ProfessorName { get; set; } = string.Empty;

        // Número do ticket em atendimento no momento.
        public string? TicketNumber { get; set; }

        // Data/hora UTC de início do atendimento atual.
        public DateTime? ServiceStartedAtUtc { get; set; }
    }

    // Item de fila exibido no dashboard.
    public class DashboardQueueItemViewModel
    {
        // Número do ticket na fila.
        public string TicketNumber { get; set; } = string.Empty;

        // Nome do aluno do chamado.
        public string StudentName { get; set; } = string.Empty;

        // Prioridade textual para leitura rápida.
        public string Priority { get; set; } = string.Empty;

        // Pontuação de ordenação na fila.
        public int QueueScore { get; set; }
    }

    // Item de atendimento finalizado para histórico recente.
    public class FinishedAttendanceViewModel
    {
        // Nome do professor que finalizou o atendimento.
        public string ProfessorName { get; set; } = string.Empty;

        // Número do ticket finalizado.
        public string TicketNumber { get; set; } = string.Empty;

        // Data/hora UTC de finalização.
        public DateTime ServiceFinishedAtUtc { get; set; }

        // Tempo total de atendimento em minutos.
        public int TotalServiceMinutes { get; set; }
    }

    // Item genérico de gráfico com rótulo e valor.
    public class DashboardChartItemViewModel
    {
        // Nome da dimensão exibida no gráfico.
        public string Label { get; set; } = string.Empty;

        // Valor numérico correspondente à dimensão.
        public int Value { get; set; }
    }

    // Resumo dos principais números de SLA.
    public class DashboardSlaOverviewViewModel
    {
        // Chamados pendentes com SLA vencido.
        public int PendingOverdueCount { get; set; }

        // Chamados pendentes ainda dentro do SLA.
        public int PendingOnTimeCount { get; set; }

        // Atendimentos finalizados dentro do SLA.
        public int FinishedWithinSlaCount { get; set; }

        // Atendimentos finalizados fora do SLA.
        public int FinishedOutOfSlaCount { get; set; }
    }

    // Série diária de aberturas e finalizações de chamados.
    public class DashboardDailyFlowViewModel
    {
        // Dia de referência do ponto temporal.
        public DateTime DayUtc { get; set; }

        // Quantidade de chamados abertos no dia.
        public int OpenedCount { get; set; }

        // Quantidade de chamados finalizados no dia.
        public int FinishedCount { get; set; }
    }
}
