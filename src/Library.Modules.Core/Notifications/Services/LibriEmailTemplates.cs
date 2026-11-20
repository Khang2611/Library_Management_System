using System.Net;

namespace Library.Modules.Core.Notifications.Services;

public static class LibriEmailTemplates
{
    private const string BrandColor = "#073D2B";
    private const string CanvasBackground = "#f6f8fa";
    private const string CardBackground = "#ffffff";
    private const string BorderColor = "#d0d7de";
    private const string TextColor = "#24292f";
    private const string MutedTextColor = "#57606a";
    private const string DividerColor = "#eaeef2";

    public static string BuildBaseCard(string title, string bodyContent)
    {
        var safeTitle = WebUtility.HtmlEncode(title);
        return $$"""
        <!DOCTYPE html>
        <html lang="vi">
        <head>
          <meta charset="utf-8">
          <meta name="viewport" content="width=device-width, initial-scale=1.0">
          <title>{{safeTitle}}</title>
        </head>
        <body style="margin: 0; padding: 0; background-color: {{CanvasBackground}}; font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Helvetica, Arial, sans-serif; color: {{TextColor}};">
          <table role="presentation" width="100%" border="0" cellspacing="0" cellpadding="0" style="background-color: {{CanvasBackground}}; padding: 40px 16px;">
            <tr>
              <td align="center">
                <table role="presentation" border="0" cellspacing="0" cellpadding="0" style="max-width: 500px; width: 100%; background-color: {{CardBackground}}; border: 1px solid {{BorderColor}}; border-radius: 6px; overflow: hidden;">
                  <tr>
                    <td align="center" style="padding: 32px 32px 18px 32px;">
                      <span style="font-size: 26px; font-weight: 800; letter-spacing: 6px; color: {{BrandColor}};">LIBRI</span>
                      <div style="height: 1px; background-color: {{DividerColor}}; margin-top: 18px;"></div>
                    </td>
                  </tr>
                  <tr>
                    <td style="padding: 0 32px 32px 32px;">
                      {{bodyContent}}
                    </td>
                  </tr>
                </table>
                <table role="presentation" border="0" cellspacing="0" cellpadding="0" style="max-width: 500px; width: 100%; margin-top: 24px;">
                  <tr>
                    <td align="center" style="font-size: 12px; line-height: 1.5; color: #8c959f;">
                      Email này được gửi tự động từ hệ thống Thư viện Libri.<br>
                      Vui lòng không trả lời trực tiếp email này.
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

    public static string BuildRegistrationOtpEmail(string toEmail, string otpCode, string? fullName = null)
    {
        var safeFullName = WebUtility.HtmlEncode(!string.IsNullOrWhiteSpace(fullName) ? fullName.Trim() : "Bạn đọc");
        var safeOtp = WebUtility.HtmlEncode(otpCode.Trim());
        var bodyContent = $$"""
        <h1 style="font-size: 20px; font-weight: 600; color: {{TextColor}}; margin: 0 0 16px 0;">Xác thực tài khoản Libri</h1>
        <p style="font-size: 14px; line-height: 1.6; color: {{TextColor}}; margin: 0 0 16px 0;">
          Xin chào <strong>{{safeFullName}}</strong>,
        </p>
        <p style="font-size: 14px; line-height: 1.6; color: {{TextColor}}; margin: 0 0 20px 0;">
          Cảm ơn bạn đã đăng ký tài khoản tại <strong>Libri</strong>. Để hoàn tất đăng ký và kích hoạt tài khoản, vui lòng nhập mã xác thực bên dưới:
        </p>
        <div style="background-color: {{CanvasBackground}}; border: 1px solid {{BorderColor}}; border-radius: 6px; padding: 20px 16px; margin: 24px 0; text-align: center;">
          <div style="font-family: ui-monospace, Menlo, Consolas, monospace; font-size: 34px; font-weight: 700; letter-spacing: 8px; color: {{BrandColor}};">
            {{safeOtp}}
          </div>
          <div style="font-size: 12px; color: {{MutedTextColor}}; margin-top: 8px;">
            (Mã có hiệu lực trong vòng 3 phút)
          </div>
        </div>
        <p style="font-size: 13px; line-height: 1.5; color: {{MutedTextColor}}; margin: 0 0 24px 0;">
          Mã xác thực này chỉ có hiệu lực trong vòng <strong>3 phút</strong>. Tuyệt đối không chia sẻ mã này với bất kỳ ai để bảo vệ tài khoản của bạn.
        </p>
        <div style="height: 1px; background-color: {{DividerColor}}; margin: 24px 0;"></div>
        <p style="font-size: 14px; line-height: 1.5; color: {{TextColor}}; margin: 0;">
          Trân trọng,<br>
          <strong>Đội ngũ Libri</strong>
        </p>
        """;

        return BuildBaseCard("Xác thực tài khoản Libri", bodyContent);
    }

    public static string BuildPasswordResetEmail(string toEmail, string otpCode, string resetLink, string? fullName = null)
    {
        var safeFullName = WebUtility.HtmlEncode(!string.IsNullOrWhiteSpace(fullName) ? fullName.Trim() : "Bạn đọc");
        var safeEmail = WebUtility.HtmlEncode(toEmail.Trim());
        var safeOtp = WebUtility.HtmlEncode(otpCode.Trim());
        var safeResetLink = WebUtility.HtmlEncode(resetLink);
        var bodyContent = $$"""
        <h1 style="font-size: 20px; font-weight: 600; color: {{TextColor}}; margin: 0 0 16px 0;">Đặt lại mật khẩu Libri</h1>
        <p style="font-size: 14px; line-height: 1.6; color: {{TextColor}}; margin: 0 0 16px 0;">
          Xin chào <strong>{{safeFullName}}</strong>,
        </p>
        <p style="font-size: 14px; line-height: 1.6; color: {{TextColor}}; margin: 0 0 20px 0;">
          Hệ thống vừa nhận được yêu cầu đặt lại mật khẩu cho tài khoản liên kết với địa chỉ email <strong>{{safeEmail}}</strong>. Bạn có thể sử dụng mã OTP gồm 6 chữ số bên dưới:
        </p>
        <div style="background-color: {{CanvasBackground}}; border: 1px solid {{BorderColor}}; border-radius: 6px; padding: 20px 16px; margin: 24px 0; text-align: center;">
          <div style="font-family: ui-monospace, Menlo, Consolas, monospace; font-size: 34px; font-weight: 700; letter-spacing: 8px; color: {{BrandColor}};">
            {{safeOtp}}
          </div>
          <div style="font-size: 12px; color: {{MutedTextColor}}; margin-top: 8px;">
            (Mã có hiệu lực trong vòng 15 phút)
          </div>
        </div>
        <p style="font-size: 14px; line-height: 1.6; color: {{TextColor}}; margin: 0 0 16px 0;">
          Hoặc bạn có thể bấm trực tiếp vào nút bên dưới để tiến hành đặt lại mật khẩu:
        </p>
        <table role="presentation" border="0" cellspacing="0" cellpadding="0" style="margin: 16px 0 20px 0;">
          <tr>
            <td align="center" style="border-radius: 6px; background-color: {{BrandColor}};">
              <a href="{{safeResetLink}}" target="_blank" style="display: inline-block; padding: 11px 24px; font-size: 14px; font-weight: 600; color: #ffffff; text-decoration: none; border-radius: 6px; background-color: {{BrandColor}};">
                Đặt lại mật khẩu
              </a>
            </td>
          </tr>
        </table>
        <p style="font-size: 12px; line-height: 1.5; color: {{MutedTextColor}}; word-break: break-all; margin: 0 0 20px 0;">
          Nếu nút bấm trên không hoạt động, bạn có thể sao chép liên kết sau vào trình duyệt:<br>
          <a href="{{safeResetLink}}" style="color: {{BrandColor}}; text-decoration: underline;">{{safeResetLink}}</a>
        </p>
        <div style="height: 1px; background-color: {{DividerColor}}; margin: 24px 0;"></div>
        <p style="font-size: 14px; line-height: 1.5; color: {{TextColor}}; margin: 0;">
          Trân trọng,<br>
          <strong>Đội ngũ Libri</strong>
        </p>
        """;

        return BuildBaseCard("Đặt lại mật khẩu Libri", bodyContent);
    }
}

