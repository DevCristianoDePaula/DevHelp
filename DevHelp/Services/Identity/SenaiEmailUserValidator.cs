using DevHelp.Models.Identity;
using Microsoft.AspNetCore.Identity;

namespace DevHelp.Services.Identity
{
    // Validador de usuário que garante domínio institucional conforme o tipo de perfil.
    public class SenaiEmailUserValidator(IEmailDomainPolicy emailDomainPolicy) : IUserValidator<ApplicationUser>
    {
        // Executa validações de usuário antes da persistência no Identity.
        public Task<IdentityResult> ValidateAsync(UserManager<ApplicationUser> manager, ApplicationUser user)
        {
            // Valida se existe um usuário para processar.
            if (user is null)
            {
                return Task.FromResult(IdentityResult.Failed(new IdentityError
                {
                    Code = "UserNull",
                    Description = "Usuário inválido para validação."
                }));
            }

            // Valida se o e-mail atende ao domínio permitido para o perfil selecionado.
            if (!emailDomainPolicy.IsEmailAllowedForUserType(user.Email ?? string.Empty, user.UserType))
            {
                return Task.FromResult(IdentityResult.Failed(new IdentityError
                {
                    Code = "InvalidInstitutionalDomain",
                    Description = "O e-mail informado não pertence aos domínios permitidos para o perfil selecionado."
                }));
            }

            // Retorna sucesso quando todas as validações são satisfeitas.
            return Task.FromResult(IdentityResult.Success);
        }
    }
}
