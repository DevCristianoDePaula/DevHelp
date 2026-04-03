using System.ComponentModel.DataAnnotations;
using DevHelp.Models.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DevHelp.Pages.Profile
{
    // Exige autenticação para edição de perfil.
    [Authorize]
    public class CompleteModel(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager) : PageModel
    {
        // Permite obter e atualizar dados do usuário autenticado.
        private readonly UserManager<ApplicationUser> _userManager = userManager;
        // Permite atualizar cookie de autenticação após mudança de dados.
        private readonly SignInManager<ApplicationUser> _signInManager = signInManager;

        // Exibe feedback da operação para atualização de perfil/senha.
        [TempData]
        public string? StatusMessage { get; set; }

        // Mostra e-mail cadastrado apenas para leitura.
        public string Email { get; private set; } = string.Empty;

        // Modelo que recebe os dados de formulário.
        [BindProperty]
        public InputModel Input { get; set; } = new();

        // Estrutura de dados de perfil.
        public class InputModel
        {
            // Nome completo para exibição e geração de avatar textual.
            [Required(ErrorMessage = "O nome completo é obrigatório.")]
            [StringLength(120, ErrorMessage = "Informe no máximo {1} caracteres.")]
            [Display(Name = "Nome completo")]
            public string FullName { get; set; } = string.Empty;

            // Turma do usuário para organização acadêmica.
            [Required(ErrorMessage = "A turma é obrigatória.")]
            [StringLength(40, ErrorMessage = "Informe no máximo {1} caracteres.")]
            [Display(Name = "Turma")]
            public string ClassGroup { get; set; } = string.Empty;

            // Recebe senha atual para autorizar troca de senha.
            [DataType(DataType.Password)]
            [Display(Name = "Senha atual")]
            public string? CurrentPassword { get; set; }

            // Recebe nova senha desejada pelo usuário.
            [DataType(DataType.Password)]
            [Display(Name = "Nova senha")]
            public string? NewPassword { get; set; }

            // Confirmação da nova senha para evitar erros de digitação.
            [DataType(DataType.Password)]
            [Display(Name = "Confirmar nova senha")]
            public string? ConfirmNewPassword { get; set; }
        }

        // Carrega dados atuais do usuário para edição.
        public async Task<IActionResult> OnGetAsync()
        {
            // Recupera usuário autenticado da sessão atual.
            var user = await _userManager.GetUserAsync(User);

            // Interrompe quando usuário não estiver disponível.
            if (user is null)
            {
                return NotFound("Usuário não encontrado.");
            }

            // Pré-carrega formulário com dados existentes.
            Input = new InputModel
            {
                FullName = user.FullName ?? string.Empty,
                ClassGroup = user.ClassGroup ?? string.Empty
            };

            // Exibe e-mail sem permitir edição.
            Email = user.Email ?? string.Empty;

            // Renderiza a página.
            return Page();
        }

        // Salva alterações de perfil do usuário autenticado.
        public async Task<IActionResult> OnPostAsync()
        {
            // Recupera usuário autenticado para atualização.
            var user = await _userManager.GetUserAsync(User);

            // Interrompe quando não houver usuário válido.
            if (user is null)
            {
                return NotFound("Usuário não encontrado.");
            }

            // Exibe e-mail sem permitir edição.
            Email = user.Email ?? string.Empty;

            // Mantém a tela quando houver erros de validação.
            if (!ModelState.IsValid)
            {
                return Page();
            }

            // Avalia se o usuário solicitou alteração de senha na mesma tela.
            var wantsPasswordChange = !string.IsNullOrWhiteSpace(Input.CurrentPassword);

            // Processa troca de senha somente quando a senha atual for informada.
            if (wantsPasswordChange)
            {
                // Exige nova senha para concluir troca.
                if (string.IsNullOrWhiteSpace(Input.NewPassword))
                {
                    ModelState.AddModelError("Input.NewPassword", "Informe a nova senha.");
                    return Page();
                }

                // Exige confirmação da nova senha para evitar erro de digitação.
                if (string.IsNullOrWhiteSpace(Input.ConfirmNewPassword))
                {
                    ModelState.AddModelError("Input.ConfirmNewPassword", "Confirme a nova senha.");
                    return Page();
                }

                // Garante correspondência entre nova senha e confirmação.
                if (!string.Equals(Input.NewPassword, Input.ConfirmNewPassword, StringComparison.Ordinal))
                {
                    ModelState.AddModelError("Input.ConfirmNewPassword", "A nova senha e a confirmação não conferem.");
                    return Page();
                }

                // Tenta efetivar a troca de senha usando validações do Identity.
                var passwordResult = await _userManager.ChangePasswordAsync(user, Input.CurrentPassword, Input.NewPassword);

                // Exibe erros quando a troca de senha não for concluída.
                if (!passwordResult.Succeeded)
                {
                    foreach (var error in passwordResult.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }

                    return Page();
                }
            }

            // Persiste o nome completo informado.
            user.FullName = Input.FullName.Trim();
            // Persiste a turma informada.
            user.ClassGroup = Input.ClassGroup.Trim();

            // Atualiza registro no banco de dados.
            var result = await _userManager.UpdateAsync(user);

            // Exibe erros retornados pela atualização.
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }

                return Page();
            }

            // Atualiza cookie para refletir dados atuais do usuário.
            await _signInManager.RefreshSignInAsync(user);

            // Exibe sucesso ao retornar para a própria tela de edição de perfil.
            StatusMessage = wantsPasswordChange
                ? "Perfil e senha atualizados com sucesso."
                : "Perfil atualizado com sucesso.";

            // Mantém fluxo em página única para edição de perfil e senha.
            return RedirectToPage();
        }
    }
}
