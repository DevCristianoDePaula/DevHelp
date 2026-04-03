namespace DevHelp.Services.Email
{
    // Centraliza as configurações SMTP usadas pelo serviço de envio de e-mails do sistema.
    public class GmailSmtpOptions
    {
        // Nome amigável exibido como remetente para o destinatário.
        public string FromName { get; set; } = "DevHelp";

        // E-mail remetente utilizado para autenticar e enviar mensagens.
        public string FromEmail { get; set; } = string.Empty;

        // Host SMTP do provedor de e-mail.
        public string SmtpHost { get; set; } = "smtp.gmail.com";

        // Porta SMTP do provedor (587 para STARTTLS no Gmail).
        public int SmtpPort { get; set; } = 587;

        // Usuário de autenticação SMTP (normalmente o mesmo e-mail remetente).
        public string UserName { get; set; } = string.Empty;

        // Senha de app/API key de autenticação SMTP.
        public string Password { get; set; } = string.Empty;
    }
}
