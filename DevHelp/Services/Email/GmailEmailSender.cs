using DevHelp.Models.Identity;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using MimeKit;
using System.Text.Encodings.Web;

namespace DevHelp.Services.Email
{
    // Implementa o envio real de e-mails do Identity usando SMTP do Gmail.
    public class GmailEmailSender(IOptions<GmailSmtpOptions> options) : IEmailSender<ApplicationUser>, IAppEmailSender
    {
        // Mantém as configurações SMTP carregadas do appsettings.
        private readonly GmailSmtpOptions _options = options.Value;

        // Envia o link de confirmação de conta para o usuário recém-cadastrado.
        public Task SendConfirmationLinkAsync(ApplicationUser user, string email, string confirmationLink)
        {
            // Monta o conteúdo HTML padronizado com visual moderno para confirmação de conta.
            var htmlBody = BuildActionEmail(
                title: "Confirme sua conta",
                message: "Clique no botão abaixo para confirmar seu e-mail e concluir seu acesso ao DevHelp.",
                actionText: "Confirmar conta",
                actionLink: confirmationLink,
                footerMessage: "Se você não solicitou este cadastro, ignore esta mensagem.");
            // Reutiliza o método central de envio para manter consistência.
            return SendEmailAsync(email, "DevHelp - Confirmação de conta", htmlBody);
        }

        // Envia código de confirmação de conta quando o fluxo exigir código em vez de link.
        public Task SendConfirmationCodeAsync(ApplicationUser user, string email, string confirmationCode)
        {
            // Monta conteúdo textual com o código de confirmação.
            var htmlBody = BuildCodeEmail(
                title: "Código de confirmação",
                message: "Use o código abaixo para confirmar sua conta no DevHelp.",
                code: confirmationCode);
            // Encaminha e-mail com código para o destinatário.
            return SendEmailAsync(email, "DevHelp - Código de confirmação", htmlBody);
        }

        // Envia o link de redefinição de senha solicitado pelo fluxo de recuperação.
        public Task SendPasswordResetLinkAsync(ApplicationUser user, string email, string resetLink)
        {
            // Monta o conteúdo HTML de recuperação de senha.
            var htmlBody = BuildActionEmail(
                title: "Redefinição de senha",
                message: "Recebemos uma solicitação para redefinir sua senha. Use o botão abaixo para continuar com segurança.",
                actionText: "Redefinir senha",
                actionLink: resetLink,
                footerMessage: "Se você não solicitou a redefinição, ignore esta mensagem.");
            // Encaminha a mensagem com o link seguro para troca de senha.
            return SendEmailAsync(email, "DevHelp - Redefinição de senha", htmlBody);
        }

        // Envia código de redefinição de senha quando o fluxo exigir código direto.
        public Task SendPasswordResetCodeAsync(ApplicationUser user, string email, string resetCode)
        {
            // Monta conteúdo textual com o código de reset.
            var htmlBody = BuildCodeEmail(
                title: "Código de redefinição",
                message: "Use o código abaixo para redefinir sua senha no DevHelp.",
                code: resetCode);
            // Encaminha o e-mail com o código de redefinição.
            return SendEmailAsync(email, "DevHelp - Código de redefinição de senha", htmlBody);
        }

        // Monta template HTML moderno com card e botão principal para ação.
        private static string BuildActionEmail(string title, string message, string actionText, string actionLink, string footerMessage)
        {
            var encodedTitle = HtmlEncoder.Default.Encode(title);
            var encodedMessage = HtmlEncoder.Default.Encode(message);
            var encodedActionText = HtmlEncoder.Default.Encode(actionText);
            var encodedFooter = HtmlEncoder.Default.Encode(footerMessage);
            var encodedLink = HtmlEncoder.Default.Encode(actionLink);

            return $"""
<!doctype html>
<html lang="pt-BR">
<body style="margin:0;padding:0;background:#f1f5f9;font-family:'Segoe UI',Arial,sans-serif;color:#0f172a;">
  <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="padding:28px 12px;">
    <tr>
      <td align="center">
        <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="max-width:620px;background:#ffffff;border:1px solid #e2e8f0;border-radius:18px;box-shadow:0 14px 36px rgba(2,8,23,0.12);overflow:hidden;">
          <tr>
            <td style="padding:30px 30px 12px 30px;">
              <div style="display:inline-block;background:#eff6ff;color:#1d4ed8;border:1px solid #bfdbfe;border-radius:999px;padding:6px 12px;font-size:12px;font-weight:700;letter-spacing:.02em;">DevHelp</div>
              <h1 style="margin:14px 0 10px 0;font-size:24px;line-height:1.25;color:#0f172a;">{encodedTitle}</h1>
              <p style="margin:0 0 20px 0;font-size:15px;line-height:1.65;color:#334155;">{encodedMessage}</p>
            </td>
          </tr>
          <tr>
            <td align="center" style="padding:0 30px 24px 30px;">
              <table role="presentation" cellspacing="0" cellpadding="0" border="0" style="margin:0 auto;">
                <tr>
                  <td align="center" bgcolor="#2563eb" style="border-radius:12px;">
                    <a href="{encodedLink}" style="display:inline-block;padding:12px 24px;color:#ffffff;text-decoration:none;font-weight:700;font-size:15px;line-height:1.2;border-radius:12px;background-color:#2563eb;">{encodedActionText}</a>
                  </td>
                </tr>
              </table>
            </td>
          </tr>
          <tr>
            <td style="padding:0 30px 26px 30px;">
              <p style="margin:0;padding:12px 14px;background:#f8fafc;border:1px solid #e2e8f0;border-radius:10px;font-size:13px;line-height:1.6;color:#475569;">{encodedFooter}</p>
            </td>
          </tr>
        </table>
      </td>
    </tr>
  </table>
</body>
</html>
""";
        }

        // Monta template HTML moderno para fluxos com código numérico/textual.
        private static string BuildCodeEmail(string title, string message, string code)
        {
            var encodedTitle = HtmlEncoder.Default.Encode(title);
            var encodedMessage = HtmlEncoder.Default.Encode(message);
            var encodedCode = HtmlEncoder.Default.Encode(code);

            return $"""
<!doctype html>
<html lang="pt-BR">
<body style="margin:0;padding:0;background:#f1f5f9;font-family:'Segoe UI',Arial,sans-serif;color:#0f172a;">
  <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="padding:28px 12px;">
    <tr>
      <td align="center">
        <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="max-width:620px;background:#ffffff;border:1px solid #e2e8f0;border-radius:18px;box-shadow:0 14px 36px rgba(2,8,23,0.12);overflow:hidden;">
          <tr>
            <td style="padding:30px;">
              <h1 style="margin:0 0 10px 0;font-size:24px;line-height:1.25;color:#0f172a;">{encodedTitle}</h1>
              <p style="margin:0 0 20px 0;font-size:15px;line-height:1.65;color:#334155;">{encodedMessage}</p>
              <div style="display:inline-block;padding:12px 18px;background:#eff6ff;border:1px solid #bfdbfe;color:#1d4ed8;font-size:24px;font-weight:700;letter-spacing:.08em;border-radius:12px;">{encodedCode}</div>
            </td>
          </tr>
        </table>
      </td>
    </tr>
  </table>
</body>
</html>
""";
        }

        // Envia o e-mail via SMTP utilizando as configurações do Gmail.
        public async Task SendEmailAsync(string toEmail, string subject, string htmlBody, IReadOnlyCollection<EmailAttachment>? attachments = null)
        {
            // Cria a mensagem MIME que será transmitida pelo SMTP.
            var message = new MimeMessage();
            // Define o remetente conforme configuração do sistema.
            message.From.Add(new MailboxAddress(_options.FromName, _options.FromEmail));
            // Define destinatário da mensagem atual.
            message.To.Add(MailboxAddress.Parse(toEmail));
            // Define assunto de identificação do e-mail.
            message.Subject = subject;
            // Define corpo em HTML com fallback em texto puro para melhor compatibilidade entre clientes de e-mail.
            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = htmlBody,
                TextBody = "Abra este e-mail em um cliente com suporte a HTML para visualizar o conteúdo completo."
            };

            if (attachments is not null)
            {
                foreach (var attachment in attachments)
                {
                    if (attachment.Content.Length == 0 || string.IsNullOrWhiteSpace(attachment.FileName))
                    {
                        continue;
                    }

                    bodyBuilder.Attachments.Add(attachment.FileName, attachment.Content, ContentType.Parse(attachment.ContentType));
                }
            }

            message.Body = bodyBuilder.ToMessageBody();

            // Cria cliente SMTP para conexão e envio da mensagem.
            using var smtpClient = new SmtpClient();
            // Conecta ao host SMTP do Gmail usando STARTTLS.
            await smtpClient.ConnectAsync(_options.SmtpHost, _options.SmtpPort, SecureSocketOptions.StartTls);
            // Autentica com usuário e senha de app configurados.
            await smtpClient.AuthenticateAsync(_options.UserName, _options.Password);
            // Envia a mensagem para o servidor SMTP.
            await smtpClient.SendAsync(message);
            // Encerra a conexão SMTP com fechamento limpo.
            await smtpClient.DisconnectAsync(true);
        }
    }
}
