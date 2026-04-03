using System.ComponentModel.DataAnnotations;
using DevHelp.Models.Admin;
using DevHelp.Models.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace DevHelp.Models.Tickets
{
    // Define os níveis de prioridade aceitos na fila de atendimento.
    public enum TicketPriority
    {
        Low = 1,
        Medium = 2,
        High = 3,
        Urgent = 4
    }

    // Define os status possíveis do ciclo de vida do chamado.
    public enum TicketStatus
    {
        Open = 1,
        InProgress = 2,
        Resolved = 3,
        Closed = 4,
        Cancelled = 5
    }

    // Representa um chamado aberto por aluno para atendimento de suporte.
    [Index(nameof(TicketNumber), IsUnique = true)]
    [Index(nameof(CreatedAtUtc))]
    public class Ticket
    {
        // Identificador interno do chamado.
        public int Id { get; set; }

        // Número público sequencial no padrão AAAA-MM-000001.
        [Required]
        [StringLength(20)]
        public string TicketNumber { get; set; } = string.Empty;

        // Identificador do aluno responsável pela abertura.
        [Required]
        public string StudentId { get; set; } = string.Empty;

        // Categoria selecionada para classificação inicial.
        [Required]
        public int CategoryId { get; set; }

        // Prioridade informada pelo aluno no momento da abertura.
        [Required]
        public TicketPriority Priority { get; set; } = TicketPriority.Medium;

        // Descrição detalhada do problema relatado.
        [Required]
        [StringLength(4000)]
        public string Description { get; set; } = string.Empty;

        // Status atual do chamado no fluxo de atendimento.
        [Required]
        public TicketStatus Status { get; set; } = TicketStatus.Open;

        // Data/hora UTC de abertura para rastreio e ordenação da fila.
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        // Data/hora UTC alvo para primeira resposta conforme prioridade.
        public DateTime ResponseDueAtUtc { get; set; }

        // Professor preferencial escolhido pelo aluno para atendimento (opcional).
        public string? PreferredProfessorId { get; set; }

        // Professor responsável pelo atendimento atual do chamado.
        public string? AssignedProfessorId { get; set; }

        // Data/hora UTC de início do atendimento em curso.
        public DateTime? ServiceStartedAtUtc { get; set; }

        // Data/hora UTC de finalização do atendimento mais recente.
        public DateTime? ServiceFinishedAtUtc { get; set; }

        // Navegação para o usuário dono do chamado.
        public ApplicationUser? Student { get; set; }

        // Navegação para categoria vinculada ao chamado.
        public Category? Category { get; set; }

        // Navegação para professor responsável quando houver atendimento ativo/finalizado.
        public ApplicationUser? AssignedProfessor { get; set; }

        // Navegação para professor preferencial selecionado pelo aluno.
        public ApplicationUser? PreferredProfessor { get; set; }

        // Coleção de anexos físicos ou links externos relacionados ao chamado.
        public ICollection<TicketAttachment> Attachments { get; set; } = [];

        // Coleção de interações de comentários no chamado.
        public ICollection<TicketComment> Comments { get; set; } = [];
    }

    // Representa um anexo de arquivo ou link externo associado ao chamado.
    public class TicketAttachment
    {
        // Identificador interno do anexo.
        public int Id { get; set; }

        // Identificador do chamado associado.
        [Required]
        public int TicketId { get; set; }

        // Nome original do arquivo enviado pelo aluno.
        [StringLength(255)]
        public string? OriginalFileName { get; set; }

        // Caminho relativo para acesso ao arquivo salvo no servidor.
        [StringLength(500)]
        public string? RelativePath { get; set; }

        // Link externo informado no formulário do chamado.
        [StringLength(1000)]
        public string? ExternalUrl { get; set; }

        // Tipo de conteúdo do arquivo quando houver upload.
        [StringLength(200)]
        public string? ContentType { get; set; }

        // Tamanho em bytes do arquivo anexado.
        public long? SizeBytes { get; set; }

        // Navegação para o chamado pai.
        public Ticket? Ticket { get; set; }
    }

    // Representa uma mensagem de interação registrada no chamado.
    public class TicketComment
    {
        // Identificador interno do comentário.
        public int Id { get; set; }

        // Identificador do chamado relacionado.
        [Required]
        public int TicketId { get; set; }

        // Identificador do autor do comentário.
        [Required]
        public string AuthorId { get; set; } = string.Empty;

        // Mensagem enviada no histórico do chamado.
        [Required]
        [StringLength(2000)]
        public string Message { get; set; } = string.Empty;

        // Data/hora UTC do registro do comentário.
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        // Navegação para chamado pai.
        public Ticket? Ticket { get; set; }

        // Navegação para usuário autor.
        public ApplicationUser? Author { get; set; }
    }

    // Representa um anúncio público de chamada de ticket para o painel da TV.
    [Index(nameof(CalledAtUtc))]
    public class TicketCallAnnouncement
    {
        // Identificador sequencial do anúncio.
        public int Id { get; set; }

        // Número do ticket chamado para atendimento.
        [Required]
        [StringLength(20)]
        public string TicketNumber { get; set; } = string.Empty;

        // Nome do aluno chamado no painel público.
        [Required]
        [StringLength(200)]
        public string StudentName { get; set; } = string.Empty;

        // Data/hora UTC em que a chamada foi disparada.
        public DateTime CalledAtUtc { get; set; } = DateTime.UtcNow;
    }

    // Define estados do processamento assíncrono de exportação de relatório em PDF.
    public enum AttendanceReportExportStatus
    {
        Pending = 1,
        Processing = 2,
        Completed = 3,
        Failed = 4
    }

    // Representa uma solicitação de exportação de histórico de atendimentos em PDF.
    [Index(nameof(RequesterId), nameof(CreatedAtUtc))]
    [Index(nameof(Status), nameof(CreatedAtUtc))]
    public class AttendanceReportExportJob
    {
        // Identificador sequencial da solicitação.
        public int Id { get; set; }

        // Usuário que solicitou o relatório.
        [Required]
        public string RequesterId { get; set; } = string.Empty;

        // Estado atual do processamento em segundo plano.
        public AttendanceReportExportStatus Status { get; set; } = AttendanceReportExportStatus.Pending;

        // Termo de busca aplicado na solicitação.
        [StringLength(200)]
        public string? SearchTerm { get; set; }

        // Categoria filtrada na solicitação.
        public int? CategoryId { get; set; }

        // Prioridade filtrada na solicitação.
        [StringLength(20)]
        public string? PriorityFilter { get; set; }

        // Início do range de finalização filtrado.
        public DateTime? DateFromUtc { get; set; }

        // Fim do range de finalização filtrado.
        public DateTime? DateToUtc { get; set; }

        // Data/hora UTC de criação do job.
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        // Data/hora UTC de início do processamento.
        public DateTime? StartedAtUtc { get; set; }

        // Data/hora UTC de finalização do processamento.
        public DateTime? CompletedAtUtc { get; set; }

        // Nome de arquivo gerado para download.
        [StringLength(255)]
        public string? OutputFileName { get; set; }

        // Caminho relativo interno do arquivo gerado.
        [StringLength(500)]
        public string? RelativePath { get; set; }

        // Mensagem de erro quando o processamento falhar.
        [StringLength(1000)]
        public string? ErrorMessage { get; set; }

        // Navegação para o usuário solicitante.
        public ApplicationUser? Requester { get; set; }
    }

    // Modelo de entrada da tela de abertura de chamado para alunos.
    public class TicketCreateViewModel
    {
        // Categoria selecionada para o novo chamado.
        [Required(ErrorMessage = "Selecione uma categoria.")]
        [Display(Name = "Categoria")]
        public int? CategoryId { get; set; }

        // Prioridade selecionada para priorização inicial.
        [Required(ErrorMessage = "Selecione a prioridade.")]
        [Display(Name = "Prioridade")]
        public TicketPriority? Priority { get; set; }

        // Descrição principal do problema enfrentado.
        [Required(ErrorMessage = "Descreva o problema.")]
        [StringLength(4000, ErrorMessage = "A descrição deve ter no máximo {1} caracteres.")]
        [Display(Name = "Descrição do problema")]
        public string Description { get; set; } = string.Empty;

        // Upload opcional de arquivos para evidências do problema.
        [Display(Name = "Arquivos")]
        public List<IFormFile>? Files { get; set; }

        // Campo opcional para múltiplos links (um por linha).
        [Display(Name = "Links relacionados")]
        [StringLength(4000, ErrorMessage = "Informe no máximo {1} caracteres em links.")]
        public string? Links { get; set; }

        // Professor preferencial para atendimento do chamado (opcional).
        [Display(Name = "Professor para atender (opcional)")]
        public string? PreferredProfessorId { get; set; }

        // Opções de categoria usadas no select da interface.
        public IReadOnlyCollection<SelectListItem> CategoryOptions { get; set; } = [];

        // Opções de prioridade usadas no select da interface.
        public IReadOnlyCollection<SelectListItem> PriorityOptions { get; set; } = [];

        // Opções de professores para seleção opcional do aluno.
        public IReadOnlyCollection<SelectListItem> ProfessorOptions { get; set; } = [];
    }

    // Modelo de exibição para listagem de chamados do aluno.
    public class TicketListItemViewModel
    {
        // Identificador interno usado em links para detalhes.
        public int Id { get; set; }

        // Número público do chamado.
        public string TicketNumber { get; set; } = string.Empty;

        // Nome da categoria vinculada.
        public string CategoryName { get; set; } = string.Empty;

        // Prioridade atual do chamado.
        public TicketPriority Priority { get; set; }

        // Status atual do chamado.
        public TicketStatus Status { get; set; }

        // Data/hora UTC de criação.
        public DateTime CreatedAtUtc { get; set; }

        // Data/hora UTC alvo para primeira resposta (SLA).
        public DateTime ResponseDueAtUtc { get; set; }

        // Quantidade total de anexos e links cadastrados.
        public int AttachmentsCount { get; set; }

        // Nome do professor preferencial escolhido pelo aluno (quando houver).
        public string? PreferredProfessorName { get; set; }
    }

    // Modelo de exibição para fila ordenada de atendimento.
    public class TicketQueueItemViewModel : TicketListItemViewModel
    {
        // Nome do aluno que abriu o chamado.
        public string StudentName { get; set; } = string.Empty;

        // Pontuação da fila combinando prioridade e tempo de espera.
        public int QueueScore { get; set; }

        // Indica se o SLA do chamado está vencido.
        public bool IsOverdue { get; set; }

        // Quantidade de minutos em atraso quando houver vencimento.
        public int OverdueMinutes { get; set; }

        // Indica que o chamado está reservado para outro professor.
        public bool IsReservedForAnotherProfessor { get; set; }
    }

    // Modelo de mensagem para postagem no histórico do chamado.
    public class TicketCommentInputModel
    {
        // Conteúdo do comentário enviado no formulário.
        [Required(ErrorMessage = "Informe uma mensagem.")]
        [StringLength(2000, ErrorMessage = "A mensagem deve ter no máximo {1} caracteres.")]
        [Display(Name = "Mensagem")]
        public string Message { get; set; } = string.Empty;
    }

    // Modelo de exibição detalhada do chamado com histórico.
    public class TicketDetailsViewModel
    {
        // Identificador interno do chamado.
        public int Id { get; set; }

        // Número público do chamado.
        public string TicketNumber { get; set; } = string.Empty;

        // Nome da categoria associada.
        public string CategoryName { get; set; } = string.Empty;

        // Prioridade atual do chamado.
        public TicketPriority Priority { get; set; }

        // Status atual do chamado.
        public TicketStatus Status { get; set; }

        // Nome do aluno dono do chamado.
        public string StudentName { get; set; } = string.Empty;

        // Nome do professor preferencial escolhido pelo aluno (quando houver).
        public string? PreferredProfessorName { get; set; }

        // Descrição original do problema.
        public string Description { get; set; } = string.Empty;

        // Data/hora UTC de abertura.
        public DateTime CreatedAtUtc { get; set; }

        // Data/hora UTC prevista para primeira resposta (SLA).
        public DateTime ResponseDueAtUtc { get; set; }

        // Indica se o chamado está com SLA vencido.
        public bool IsOverdue { get; set; }

        // Minutos em atraso no SLA quando aplicável.
        public int OverdueMinutes { get; set; }

        // Lista de anexos e links do chamado.
        public IReadOnlyCollection<TicketAttachment> Attachments { get; set; } = [];

        // Lista de comentários em ordem cronológica.
        public IReadOnlyCollection<TicketCommentViewModel> Comments { get; set; } = [];

        // Modelo de entrada para novo comentário.
        public TicketCommentInputModel CommentInput { get; set; } = new();

        // Indica se o usuário atual pode alterar status.
        public bool CanChangeStatus { get; set; }
    }

    // Modelo simplificado de comentário para renderização da timeline.
    public class TicketCommentViewModel
    {
        // Nome do autor do comentário.
        public string AuthorName { get; set; } = string.Empty;

        // Mensagem registrada no histórico.
        public string Message { get; set; } = string.Empty;

        // Data/hora UTC de criação.
        public DateTime CreatedAtUtc { get; set; }
    }

    // Agrupa dados da tela de histórico de atendimentos com filtros e paginação.
    public class TicketAttendanceHistoryPageViewModel
    {
        // Itens retornados na página atual após filtros aplicados.
        public IReadOnlyCollection<TicketAttendanceHistoryItemViewModel> Items { get; set; } = [];

        // Termo de busca para aluno/docente/e-mail.
        public string SearchTerm { get; set; } = string.Empty;

        // Categoria selecionada no filtro.
        public int? CategoryId { get; set; }

        // Prioridade selecionada no filtro.
        public string PriorityFilter { get; set; } = string.Empty;

        // Data inicial para filtro por finalização.
        public DateTime? DateFrom { get; set; }

        // Data final para filtro por finalização.
        public DateTime? DateTo { get; set; }

        // Identificador do último job de exportação solicitado.
        public int? ExportJobId { get; set; }

        // Opções de categoria para filtro.
        public IReadOnlyCollection<SelectListItem> CategoryOptions { get; set; } = [];

        // Opções de prioridade para filtro.
        public IReadOnlyCollection<SelectListItem> PriorityOptions { get; set; } = [];

        // Quantidade de registros exibidos por página.
        public int PageSize { get; set; } = 10;

        // Página atual da listagem.
        public int CurrentPage { get; set; } = 1;

        // Total de páginas após filtros.
        public int TotalPages { get; set; } = 1;

        // Total de registros encontrados após filtros.
        public int TotalRecords { get; set; }
    }

    // Representa uma linha do histórico de atendimentos finalizados.
    public class TicketAttendanceHistoryItemViewModel
    {
        // Identificador interno para links de navegação.
        public int Id { get; set; }

        // Número público do chamado.
        public string TicketNumber { get; set; } = string.Empty;

        // Nome do aluno do chamado.
        public string StudentName { get; set; } = string.Empty;

        // E-mail do aluno do chamado.
        public string StudentEmail { get; set; } = string.Empty;

        // Nome do docente que atendeu.
        public string ProfessorName { get; set; } = string.Empty;

        // E-mail do docente que atendeu.
        public string ProfessorEmail { get; set; } = string.Empty;

        // Nome da categoria do chamado.
        public string CategoryName { get; set; } = string.Empty;

        // Status final do chamado no momento da listagem.
        public TicketStatus Status { get; set; }

        // Prioridade do chamado.
        public TicketPriority Priority { get; set; }

        // Data/hora UTC de finalização.
        public DateTime ServiceFinishedAtUtc { get; set; }

        // Tempo total de atendimento em minutos.
        public int TotalServiceMinutes { get; set; }
    }
}
