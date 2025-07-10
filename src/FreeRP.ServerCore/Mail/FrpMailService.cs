using FreeRP.FrpServices;
using FreeRP.Settings;
using MimeKit;

namespace FreeRP.ServerCore.Mail
{
    public class FrpMailService(IFrpDataService ds)
    {
        private readonly IFrpDataService _ds = ds;

        public async ValueTask<bool> TrySendMailAsync(FrpSettings frpSettings, string mailAddress, string subject, string body, IFrpAuthService auth)
        {
            try
            {
                MimeMessage msg = new();
                msg.From.Add(InternetAddress.Parse(frpSettings.SmtpSettings.EMail));
                msg.To.Add(InternetAddress.Parse(mailAddress));
                msg.Subject = subject;
                msg.Body = new TextPart(body);

                using var smtp = new MailKit.Net.Smtp.SmtpClient();
                await smtp.ConnectAsync(frpSettings.SmtpSettings.Host, frpSettings.SmtpSettings.Port);
                await smtp.AuthenticateAsync(frpSettings.SmtpSettings.Username, frpSettings.SmtpSettings.Password);
                await smtp.SendAsync(msg);
                await smtp.DisconnectAsync(true);

                return true;
            }
            catch (Exception ex)
            {
                await AddExceptionAsync(nameof(TrySendMailAsync), ex, auth);
                return false;
            }
        }

        private async ValueTask AddExceptionAsync(string location, Exception ex, IFrpAuthService authService)
        {
            await _ds.FrpLogService.AddExceptionLogAsync(new()
            {
                UtcUnixTimeSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                Location = $"{nameof(FrpMailService)}/{location}",
                Message = ex.Message,
                UserId = authService.User.UserId,
                Val1 = ex.StackTrace
            }, authService);
        }
    }
}
