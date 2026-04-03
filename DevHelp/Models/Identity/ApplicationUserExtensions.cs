namespace DevHelp.Models.Identity
{
    // Centraliza regras utilitárias relacionadas ao usuário da aplicação.
    public static class ApplicationUserExtensions
    {
        // Verifica se os campos mínimos do perfil já foram preenchidos.
        public static bool HasCompletedProfile(this ApplicationUser user)
        {
            // Retorna verdadeiro quando nome completo e turma possuem conteúdo válido.
            return !string.IsNullOrWhiteSpace(user.FullName) && !string.IsNullOrWhiteSpace(user.ClassGroup);
        }

        // Gera iniciais com base no nome completo para uso no avatar textual.
        public static string GetInitials(string? fullName, string? fallbackEmail = null)
        {
            // Usa o nome completo quando ele existir.
            if (!string.IsNullOrWhiteSpace(fullName))
            {
                // Divide o nome em partes removendo espaços extras.
                var parts = fullName
                    .Trim()
                    .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                // Se houver apenas um nome, repete a primeira letra para manter padrão de 2 caracteres.
                if (parts.Length == 1)
                {
                    return char.ToUpperInvariant(parts[0][0]).ToString();
                }

                // Retorna primeira letra do primeiro e do último nome.
                return string.Concat(char.ToUpperInvariant(parts[0][0]), char.ToUpperInvariant(parts[^1][0]));
            }

            // Usa o e-mail como fallback quando não houver nome completo.
            if (!string.IsNullOrWhiteSpace(fallbackEmail))
            {
                // Extrai o identificador antes do arroba.
                var localPart = fallbackEmail.Split('@')[0];

                // Retorna primeira letra do identificador.
                if (!string.IsNullOrWhiteSpace(localPart))
                {
                    return char.ToUpperInvariant(localPart[0]).ToString();
                }
            }

            // Retorna padrão quando não existir dado suficiente.
            return "U";
        }
    }
}
