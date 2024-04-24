using HandlebarsDotNet;
using MailKit.Net.Smtp;
using MimeKit;
using Serilog;
using TechNews.Common.Library.MessageBus;
using TechNews.Common.Library.Messages.Events;
using TechNews.Services.Notification.Configurations;
using TechNews.Services.Notification.Email.Templates.EmailConfirmation;

namespace TechNews.Services.Notification;

public class Worker
(
    IMessageBus bus
) : BackgroundService
{
    private readonly string HostAndPort = EnvironmentVariables.WebHostAndPort;
    private readonly string LogoUrl = EnvironmentVariables.LogoUrl;

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Log.Information("Worker has started");
        bus.Consume<UserRegisteredEvent>(EnvironmentVariables.BrokerConfirmEmailQueueName, ExecuteAfterConsumed);
        return Task.CompletedTask;
    }

    private void ExecuteAfterConsumed(UserRegisteredEvent? message)
    {
        Log.Information("New message received: {@message}", message);

        if (message is null)
        {
            Log.Warning("Message is null. Skipping e-mail notification");
            return;
        }

        try
        {
            var mailMessage = GenerateMessage(message);
            SendEmail(mailMessage);
        }
        catch (Exception e)
        {
            Log.Error(e, "Error while sending notification");
            throw;
        }
    }

    private MimeMessage GenerateMessage(UserRegisteredEvent userRegisteredDetails)
    {
        var mailMessage = new MimeMessage();
        mailMessage.From.Add(new MailboxAddress("TechNews", EnvironmentVariables.SmtpEmail));
        mailMessage.To.Add(new MailboxAddress(userRegisteredDetails.UserName, userRegisteredDetails.Email));
        mailMessage.Subject = "TechNews - Email confirmation";
        
        var bodyBuilder = new BodyBuilder
        {
            HtmlBody = GetHtmlTemplate(
                new EmailConfirmationTemplateModel(
                    HostAndPort: HostAndPort, 
                    LogoUrl: LogoUrl, 
                    UserName: userRegisteredDetails.UserName ?? userRegisteredDetails.Email, 
                    EmailBase64: Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(userRegisteredDetails.Email)),
                    ValidateEmailTokenBase64: Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(userRegisteredDetails.ValidateEmailToken))
                )
            )
        };

        mailMessage.Body = bodyBuilder.ToMessageBody();

        return mailMessage;
    }

    private void SendEmail(MimeMessage message)
    {
        using var client = new SmtpClient();
        client.Connect(EnvironmentVariables.SmtpHost, EnvironmentVariables.SmtpPort, false);
        client.AuthenticationMechanisms.Remove("XOAUTH2");
        client.Authenticate(EnvironmentVariables.SmtpEmail, EnvironmentVariables.SmtpPassword);

        client.Send(message);
        client.Disconnect(true);
    }

    private static string GetHtmlTemplate(EmailConfirmationTemplateModel emailConfirmationTemplateModel)
    {
        //TODO: check if it will work on an app running after publish
        // var sourceTemplate = File.ReadAllText("Templates/EmailConfirmation/EmailConfirmationTemplate.html");
        var template = Handlebars.Compile(GetHtmlTemplate());

        return template(emailConfirmationTemplateModel);
    }

    private static string GetHtmlTemplate()
    {
       return @"
            <!doctype html>
            <meta charset=utf-8>
            <meta content=""ie=edge"" http-equiv=x-ua-compatible><title>TechNews - Confirmação de Email</title>
            <meta content=""width=device-width,initial-scale=1"" name=viewport>
            <style>
                @media screen {
                    @font-face {
                        font-family: 'Source Sans Pro';
                        font-style: normal;
                        font-weight: 400;
                        src: local('Source Sans Pro Regular'), local('SourceSansPro-Regular'), url(https://fonts.gstatic.com/s/sourcesanspro/v10/ODelI1aHBYDBqgeIAH2zlBM0YzuT7MdOe03otPbuUS0.woff) format('woff')
                    }
                    @font-face {
                        font-family: 'Source Sans Pro';
                        font-style: normal;
                        font-weight: 700;
                        src: local('Source Sans Pro Bold'), local('SourceSansPro-Bold'), url(https://fonts.gstatic.com/s/sourcesanspro/v10/toadOcfmlt9b38dHJxOBGFkQc6VGVFSmCnC_l7QZG60.woff) format('woff')
                    }
                    a, body, table, td {
                        -ms-text-size-adjust: 100%;
                        -webkit-text-size-adjust: 100%
                    }
                    table, td {
                        mso-table-rspace: 0;
                        mso-table-lspace: 0
                    }
                    img {
                        -ms-interpolation-mode: bicubic
                    }
                    a[x-apple-data-detectors] {
                        font-family: inherit !important;
                        font-size: inherit !important;
                        font-weight: inherit !important;
                        line-height: inherit !important;
                        color: inherit !important;
                        text-decoration: none !important
                    }
                    div[style*=""margin: 16px 0;""] {
                        margin: 0 !important
                    }
                    body {
                        width: 100% !important;
                        height: 100% !important;
                        padding: 0 !important;
                        margin: 0 !important
                    }
                    table {
                        border-collapse: collapse !important
                    }
                    a {
                        color: #1a82e2
                    }
                    img {
                        height: auto;
                        line-height: 100%;
                        text-decoration: none;
                        border: 0;
                        outline: 0
                    }
                }
            </style>
            <body style=background-color:#e9ecef>
            <div class=preheader style=display:none;max-width:0;max-height:0;overflow:hidden;font-size:1px;line-height:1px;color:#fff;opacity:0>
                TechNews - Confirme seu e-mail.
            </div>
            <table border=0 cellpadding=0 cellspacing=0 width=100%>
                <tr>
                    <td align=center bgcolor=#e9ecef>
                        <table border=0 cellpadding=0 cellspacing=0 width=100% style=max-width:600px>
                            <tr>
                                <td align=center style=""padding:36px 24px"" valign=top>
                                    <a href=""{{hostAndPort}}"" target=_blank style=display:inline-block>
                                        <img alt=Logo src=""{{logoUrl}}"" style=display:block;width:600px;max-width:600px;min-width:600px>
                                    </a>
                                </td>
                            </tr>
                        </table>
                    </td>
                </tr>
                <tr>
                    <td align=center bgcolor=#e9ecef>
                        <table border=0 cellpadding=0 cellspacing=0 width=100% style=max-width:600px>
                            <tr>
                                <td align=left bgcolor=#ffffff style=""padding:36px 24px 0;font-family:'Source Sans Pro',Helvetica,Arial,sans-serif;border-top:3px solid #d4dadf"">
                                    <h1 style=margin:0;font-size:32px;font-weight:700;letter-spacing:-1px;line-height:48px>Olá, {{userName}}!</h1>
                                </td>
                            </tr>
                        </table>
                    </td>
                </tr>
                <tr>
                    <td align=center bgcolor=#e9ecef>
                        <table border=0 cellpadding=0 cellspacing=0 width=100% style=max-width:600px>
                            <tr>
                                <td align=left bgcolor=#ffffff style=""padding:24px;font-family:'Source Sans Pro',Helvetica,Arial,sans-serif;font-size:16px;line-height:24px"">
                                    <p style=margin:0>Clique no link abaixo para confirmar o seu e-mail. Se você não criou uma conta no <a href=""{{hostAndPort}}"">TechNews</a>, pode desconsiderar este e-mail em segurança.</p>
                                </td>
                            </tr>
                            <tr>
                                <td align=left bgcolor=#ffffff>
                                    <table border=0 cellpadding=0 cellspacing=0 width=100%>
                                        <tr>
                                            <td align=center bgcolor=#ffffff style=padding:12px>
                                                <table border=0 cellpadding=0 cellspacing=0>
                                                    <tr>
                                                        <td align=center bgcolor=#1a82e2 style=border-radius:6px>
                                                            <a href=""{{hostAndPort}}/account/email-confirmation?email={{EmailBase64}}&token={{ValidateEmailTokenBase64}}"" target=_blank style=""display:inline-block;padding:16px 36px;font-family:'Source Sans Pro',Helvetica,Arial,sans-serif;font-size:16px;color:#fff;text-decoration:none;border-radius:6px"">
                                                                Confirmar e-mail
                                                            </a>
                                                        </td>
                                                    </tr>
                                                </table>
                                            </td>
                                        </tr>
                                    </table>
                                </td>
                            <tr>
                                <td align=left bgcolor=#ffffff style=""padding:24px;font-family:'Source Sans Pro',Helvetica,Arial,sans-serif;font-size:16px;line-height:24px"">
                                    <p style=margin:0>Se o botão acima não funcionar, pode copiar o link abaixo e colar na barra de busca do seu navegador:</p>
                                    <p style=margin:0>
                                        <a href=""{{hostAndPort}}/account/email-confirmation?email={{EmailBase64}}&token={{validateEmailTokenBase64}}"" target=_blank>
                                            {{hostAndPort}}/account/email-confirmation?email={{EmailBase64}}&token={{validateEmailTokenBase64}}
                                        </a>
                                    </p>
                                </td>
                            </tr>
                            <tr>
                                <td align=left bgcolor=#ffffff style=""padding:24px;font-family:'Source Sans Pro',Helvetica,Arial,sans-serif;font-size:16px;line-height:24px;border-bottom:3px solid #d4dadf"">
                                    <p style=margin:0>Até breve,</p>
                                    <p><b>TechNews</b></p>
                                </td>
                            </tr>
                        </table>
                    </td>
                </tr>
                <tr>
                    <td align=center bgcolor=#e9ecef style=padding:24px>
                        <table border=0 cellpadding=0 cellspacing=0 width=100% style=max-width:600px>
                            <tr>
                                <td align=center bgcolor=#e9ecef style=""padding:12px 24px;font-family:'Source Sans Pro',Helvetica,Arial,sans-serif;font-size:14px;line-height:20px;color:#666"">
                                    <p style=margin:0>Você recebeu este e-mail por solicitado o cadastro no site TechNews. Se você não solicitou esse cadastro, pode desconsiderar este e-mail em segurança.</p>
                                </td>
                            </tr>
                        </table>
                    </td>
                </tr>
            </table>";
    }
}
