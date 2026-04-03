namespace DevHelp.Services.Email
{
    // Representa anexo binário para envio de e-mail transacional.
    public sealed class EmailAttachment
    {
        // Nome do arquivo anexado.
        public string FileName { get; set; } = string.Empty;

        // Tipo de conteúdo MIME do anexo.
        public string ContentType { get; set; } = "application/octet-stream";

        // Conteúdo binário do arquivo anexado.
        public byte[] Content { get; set; } = [];
    }

    // Define contrato para envio de e-mails transacionais fora dos fluxos padrão do Identity.
    public interface IAppEmailSender
    {
        // Envia e-mail HTML para destinatário informado.
        Task SendEmailAsync(string toEmail, string subject, string htmlBody, IReadOnlyCollection<EmailAttachment>? attachments = null);
    }
}
