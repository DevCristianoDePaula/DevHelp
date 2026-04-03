using System.ComponentModel.DataAnnotations;
using System.Text;
using DevHelp.Models.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;

namespace DevHelp.Areas.Identity.Pages.Account
{
    // Permite acesso anônimo para redefinição de senha via link seguro por e-mail.
    [AllowAnonymous]
    public class ResetPasswordModel(UserManager<ApplicationUser> userManager) : PageModel
    {
        // Gerencia validações e atualização de senha no Identity.
        private readonly UserManager<ApplicationUser> _userManager = userManager;

        // Modelo de entrada da redefinição de senha.
        [BindProperty]
        public InputModel Input { get; set; } = new();

        // Estrutura do formulário de redefinição.
        public class InputModel
        {
            // Código de redefinição recebido por e-mail.
            [Required]
            public string Code { get; set; } = string.Empty;

            // E-mail da conta que terá a senha redefinida.
            [Required(ErrorMessage = "O e-mail é obrigatório.")]
            [EmailAddress(ErrorMessage = "Informe um e-mail válido.")]
            [Display(Name = "E-mail")]
            public string Email { get; set; } = string.Empty;

            // Nova senha escolhida pelo usuário.
            [Required(ErrorMessage = "A nova senha é obrigatória.")]
            [StringLength(100, ErrorMessage = "A nova senha deve ter ao menos {2} e no máximo {1} caracteres.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "Nova senha")]
            public string Password { get; set; } = string.Empty;

            // Confirmação da nova senha para evitar erros de digitação.
            [Required(ErrorMessage = "A confirmação da nova senha é obrigatória.")]
            [DataType(DataType.Password)]
            [Display(Name = "Confirmar nova senha")]
            [Compare("Password", ErrorMessage = "A nova senha e a confirmação não conferem.")]
            public string ConfirmPassword { get; set; } = string.Empty;
        }

        // Carrega dados iniciais do formulário a partir do link recebido.
        public IActionResult OnGet(string? code = null, string? email = null)
        {
            // Impede acesso sem token de redefinição válido na URL.
            if (string.IsNullOrWhiteSpace(code))
            {
                return BadRequest("Código de redefinição inválido.");
            }

            // Pré-preenche campos necessários para o post.
            Input = new InputModel
            {
                Code = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code)),
                Email = email ?? string.Empty
            };

            // Renderiza formulário para definição de nova senha.
            return Page();
        }

        // Processa a redefinição de senha quando o formulário é enviado.
        public async Task<IActionResult> OnPostAsync()
        {
            // Mantém na página quando houver erros de validação do formulário.
            if (!ModelState.IsValid)
            {
                return Page();
            }

            // Busca usuário pelo e-mail informado.
            var user = await _userManager.FindByEmailAsync(Input.Email);

            // Evita exposição de existência de conta e mantém fluxo padrão.
            if (user is null)
            {
                return RedirectToPage("./ResetPasswordConfirmation");
            }

            // Executa redefinição de senha com token previamente decodificado.
            var result = await _userManager.ResetPasswordAsync(user, Input.Code, Input.Password);

            // Redireciona para confirmação quando operação for concluída.
            if (result.Succeeded)
            {
                return RedirectToPage("./ResetPasswordConfirmation");
            }

            // Exibe mensagens retornadas pelo Identity no formulário.
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return Page();
        }
    }
}
