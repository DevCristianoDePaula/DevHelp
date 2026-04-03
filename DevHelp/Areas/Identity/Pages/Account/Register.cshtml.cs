using System.ComponentModel.DataAnnotations;
using System.Text;
using DevHelp.Models.Identity;
using DevHelp.Services.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;

namespace DevHelp.Areas.Identity.Pages.Account
{
    // Permite acesso anônimo à página de cadastro.
    [AllowAnonymous]
    public class RegisterModel(
        UserManager<ApplicationUser> userManager,
        IUserStore<ApplicationUser> userStore,
        SignInManager<ApplicationUser> signInManager,
        ILogger<RegisterModel> logger,
        IEmailSender<ApplicationUser> emailSender,
        IEmailDomainPolicy emailDomainPolicy) : PageModel
    {
        // Gerencia operações de usuário (criação, senha, e-mail, etc.).
        private readonly UserManager<ApplicationUser> _userManager = userManager;
        // Fornece acesso à store do usuário do Identity.
        private readonly IUserStore<ApplicationUser> _userStore = userStore;
        // Gerencia login e autenticação do usuário.
        private readonly SignInManager<ApplicationUser> _signInManager = signInManager;
        // Registra eventos e mensagens de diagnóstico.
        private readonly ILogger<RegisterModel> _logger = logger;
        // Envia e-mail de confirmação de conta.
        private readonly IEmailSender<ApplicationUser> _emailSender = emailSender;
        // Aplica a política de domínio institucional.
        private readonly IEmailDomainPolicy _emailDomainPolicy = emailDomainPolicy;

        // Recebe e valida os dados enviados no formulário.
        [BindProperty]
        public InputModel Input { get; set; } = new();

        // Guarda a URL de retorno após o cadastro.
        public string? ReturnUrl { get; set; }

        // Lista provedores de login externo (Google, Microsoft etc.).
        public IList<AuthenticationScheme> ExternalLogins { get; set; } = [];

        // Modelo de entrada da tela de cadastro.
        public class InputModel
        {
            // Captura o e-mail informado pelo usuário.
            [Required(ErrorMessage = "O e-mail é obrigatório.")]
            [EmailAddress(ErrorMessage = "Informe um e-mail válido.")]
            [Display(Name = "E-mail")]
            public string Email { get; set; } = string.Empty;

            // Captura a senha desejada para autenticação.
            [Required(ErrorMessage = "A senha é obrigatória.")]
            [StringLength(100, ErrorMessage = "A senha deve ter ao menos {2} e no máximo {1} caracteres.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "Senha")]
            public string Password { get; set; } = string.Empty;

            // Captura a confirmação da senha para evitar erros de digitação.
            [DataType(DataType.Password)]
            [Display(Name = "Confirmar senha")]
            [Compare("Password", ErrorMessage = "A senha e a confirmação não conferem.")]
            public string ConfirmPassword { get; set; } = string.Empty;
        }

        // Carrega os dados necessários para renderizar o formulário.
        public async Task OnGetAsync(string? returnUrl = null)
        {
            // Define e armazena o retorno padrão da aplicação.
            ReturnUrl = returnUrl;
            // Busca provedores de autenticação externos configurados.
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
        }

        // Processa o envio do formulário de cadastro.
        public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
        {
            // Define a URL de retorno padrão para a raiz da aplicação.
            returnUrl ??= Url.Content("~/");
            // Mantém a URL em propriedade para reuso na view.
            ReturnUrl = returnUrl;
            // Recarrega provedores externos em caso de erro de validação.
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            // Valida domínio de e-mail para cadastro inicial sempre no perfil Aluno.
            if (!_emailDomainPolicy.IsEmailAllowedForUserType(Input.Email, UserType.Student))
            {
                ModelState.AddModelError("Input.Email", "O e-mail não pertence aos domínios permitidos para novos cadastros de aluno.");
            }

            // Continua apenas quando todos os dados estiverem válidos.
            if (ModelState.IsValid)
            {
                // Cria uma nova instância de usuário.
                var user = CreateUser();
                // Define o tipo inicial do usuário como aluno.
                user.UserType = UserType.Student;

                // Resolve a store de e-mail necessária para gravar o e-mail.
                var emailStore = GetEmailStore();
                // Define o nome de usuário como o próprio e-mail.
                await _userStore.SetUserNameAsync(user, Input.Email, CancellationToken.None);
                // Define o e-mail do usuário na store.
                await emailStore.SetEmailAsync(user, Input.Email, CancellationToken.None);

                // Cria o usuário com a senha informada.
                var result = await _userManager.CreateAsync(user, Input.Password);

                // Quando a criação for bem-sucedida, segue com papéis e confirmação.
                if (result.Succeeded)
                {
                    // Associa todo novo cadastro ao papel Aluno.
                    await _userManager.AddToRoleAsync(user, "Aluno");

                    // Registra evento de auditoria em log.
                    _logger.LogInformation("Novo usuário criado com senha e perfil inicial Aluno.");

                    // Gera o token de confirmação de e-mail.
                    var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                    // Converte o token para URL-safe.
                    code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));

                    // Monta a URL de confirmação para o usuário recém-cadastrado.
                    var callbackUrl = Url.Page(
                        "/Account/ConfirmEmail",
                        pageHandler: null,
                        values: new { area = "Identity", userId = user.Id, code, returnUrl },
                        protocol: Request.Scheme);

                    // Envia o link de confirmação para o e-mail informado.
                    await _emailSender.SendConfirmationLinkAsync(user, Input.Email, callbackUrl!);

                    // Redireciona para tela de confirmação quando confirmação obrigatória estiver ativa.
                    if (_userManager.Options.SignIn.RequireConfirmedAccount)
                    {
                        return RedirectToPage("RegisterConfirmation", new { email = Input.Email, returnUrl });
                    }

                    // Efetua login automático quando confirmação não for obrigatória.
                    await _signInManager.SignInAsync(user, isPersistent: false);
                    // Retorna para a URL original de navegação.
                    return LocalRedirect(returnUrl);
                }

                // Propaga erros de criação de conta para a interface.
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            // Retorna a página com mensagens de validação quando houver falha.
            return Page();
        }

        // Cria instância de ApplicationUser usando ativação padrão.
        private static ApplicationUser CreateUser()
        {
            try
            {
                // Instancia o tipo de usuário configurado no projeto.
                return Activator.CreateInstance<ApplicationUser>();
            }
            catch
            {
                // Lança erro orientando como corrigir caso o tipo não seja instanciável.
                throw new InvalidOperationException($"Não foi possível criar uma instância de '{nameof(ApplicationUser)}'. " +
                    $"Verifique se '{nameof(ApplicationUser)}' não é abstrata e possui construtor sem parâmetros.");
            }
        }

        // Obtém a store de e-mail para persistir o campo Email.
        private IUserEmailStore<ApplicationUser> GetEmailStore()
        {
            // Garante que a store atual suporte e-mail.
            if (!_userManager.SupportsUserEmail)
            {
                // Lança erro quando a configuração não suporta e-mail.
                throw new NotSupportedException("A UI padrão exige um user store com suporte a e-mail.");
            }

            // Retorna a store convertida para a interface correta.
            return (IUserEmailStore<ApplicationUser>)_userStore;
        }
    }
}
