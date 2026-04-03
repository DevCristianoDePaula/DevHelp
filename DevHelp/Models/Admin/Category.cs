using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace DevHelp.Models.Admin
{
    // Entidade de categoria usada para classificar chamados no sistema.
    [Index(nameof(Name), IsUnique = true)]
    public class Category
    {
        // Identificador único da categoria.
        public int Id { get; set; }

        // Nome de exibição da categoria no painel e nos formulários.
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        // Descrição opcional para detalhar o propósito da categoria.
        [StringLength(300)]
        public string? Description { get; set; }

        // Data de criação para auditoria e ordenação.
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        // Data da última atualização para rastreabilidade.
        public DateTime? UpdatedAtUtc { get; set; }
    }

    // ViewModel de formulário para criação e edição de categorias.
    public class CategoryFormViewModel
    {
        // Identificador da categoria em edição.
        public int Id { get; set; }

        // Nome da categoria com validação de obrigatoriedade.
        [Required(ErrorMessage = "O nome da categoria é obrigatório.")]
        [StringLength(100, ErrorMessage = "O nome deve ter no máximo {1} caracteres.")]
        [Display(Name = "Nome")]
        public string Name { get; set; } = string.Empty;

        // Descrição opcional com limite de tamanho.
        [StringLength(300, ErrorMessage = "A descrição deve ter no máximo {1} caracteres.")]
        [Display(Name = "Descrição")]
        public string? Description { get; set; }
    }

    // ViewModel de listagem paginada para o CRUD de categorias.
    public class CategoryManagementPageViewModel
    {
        // Coleção de categorias a serem exibidas na página atual.
        public IReadOnlyCollection<Category> Categories { get; set; } = [];

        // Termo de busca por nome ou descrição.
        public string SearchTerm { get; set; } = string.Empty;

        // Quantidade de itens por página.
        public int PageSize { get; set; } = 10;

        // Página atual da listagem.
        public int CurrentPage { get; set; } = 1;

        // Número total de páginas disponíveis.
        public int TotalPages { get; set; } = 1;

        // Quantidade total de registros após filtro.
        public int TotalRecords { get; set; }
    }
}
