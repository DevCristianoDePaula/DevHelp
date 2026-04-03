using DevHelp.Models.Identity;
using MimeKit;

namespace DevHelp.Services.Identity
{
    // Implementa as regras de domínio institucional aceitas para cadastro no DevHelp.
    public class SenaiEmailDomainPolicy : IEmailDomainPolicy
    {
        // Lista de domínios permitidos para alunos.
        private static readonly HashSet<string> StudentDomains = new(StringComparer.OrdinalIgnoreCase)
        {
            "aluno.senai.br",
            "edu.senai.br",
            "sp.senai.br",
            "docente.senai.br"
        };

        // Lista de domínios permitidos para docentes.
        private static readonly HashSet<string> TeacherDomains = new(StringComparer.OrdinalIgnoreCase)
        {
            "sp.senai.br",
            "docente.senai.br",
            "edu.senai.br"
        };

        // Lista de domínios permitidos para administradores internos.
        private static readonly HashSet<string> AdminDomains = new(StringComparer.OrdinalIgnoreCase)
        {
            "devhelp.com.br"
        };

        // Avalia se o e-mail é válido sintaticamente e se o domínio atende à política do tipo de usuário.
        public bool IsEmailAllowedForUserType(string email, UserType userType)
        {
            // Rejeita entradas vazias para evitar validações inconsistentes.
            if (string.IsNullOrWhiteSpace(email))
            {
                return false;
            }

            // Usa MimeKit para validar o formato do e-mail e extrair o domínio com segurança.
            if (!MailboxAddress.TryParse(email.Trim(), out var mailbox) || string.IsNullOrWhiteSpace(mailbox.Address))
            {
                return false;
            }

            // Divide o endereço no separador de domínio.
            var splitEmail = mailbox.Address.Split('@');

            // Rejeita formatos inesperados sem domínio válido.
            if (splitEmail.Length != 2 || string.IsNullOrWhiteSpace(splitEmail[1]))
            {
                return false;
            }

            // Normaliza o domínio para comparação.
            var domain = splitEmail[1].Trim().ToLowerInvariant();

            // Aplica a lista correta conforme o perfil selecionado.
            return userType switch
            {
                UserType.Student => StudentDomains.Contains(domain),
                UserType.Teacher => TeacherDomains.Contains(domain),
                UserType.Admin => AdminDomains.Contains(domain),
                _ => false
            };
        }
    }
}
