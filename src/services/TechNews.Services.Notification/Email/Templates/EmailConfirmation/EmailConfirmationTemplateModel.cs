namespace TechNews.Services.Notification.Email.Templates.EmailConfirmation;

public record EmailConfirmationTemplateModel(string HostAndPort, string LogoUrl, string UserName, string EmailBase64, string ValidateEmailTokenBase64);
