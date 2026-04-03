namespace DevHelp.Models.Admin
{
    // Agrupa dados da tela administrativa de usuários com filtros e paginação.
    public class UserManagementPageViewModel
    {
        // Usuários retornados na página atual após filtros.
        public IReadOnlyCollection<UserManagementViewModel> Users { get; set; } = [];

        // Termo digitado para busca por nome ou e-mail.
        public string SearchTerm { get; set; } = string.Empty;

        // Papel selecionado para filtro no painel.
        public string RoleFilter { get; set; } = string.Empty;

        // Quantidade de registros exibidos por página.
        public int PageSize { get; set; } = 10;

        // Página atual da listagem.
        public int CurrentPage { get; set; } = 1;

        // Total de páginas calculadas após filtros.
        public int TotalPages { get; set; } = 1;

        // Total de registros encontrados após aplicar filtros.
        public int TotalRecords { get; set; }
    }

    // Representa uma linha da listagem de gerenciamento de usuários pelo administrador.
    public class UserManagementViewModel
    {
        // Identificador do usuário no Identity.
        public string UserId { get; set; } = string.Empty;

        // E-mail principal do usuário cadastrado.
        public string Email { get; set; } = string.Empty;

        // Nome para exibição no painel administrativo.
        public string DisplayName { get; set; } = string.Empty;

        // Papel principal do usuário no sistema.
        public string Role { get; set; } = "Aluno";

        // Indica se o usuário está com bloqueio ativo.
        public bool IsLocked { get; set; }

        // Indica se o e-mail da conta já foi confirmado.
        public bool EmailConfirmed { get; set; }
    }
}
