using System.Net;
using Library.Modules.Core.Notifications.Services;

namespace Library.Modules.Core.Notifications.Features.EmailHandlers;

public static class EmailTemplates
{
    private const string BrandColor = "#073D2B";
    private const string CanvasBackground = "#f6f8fa";
    private const string BorderColor = "#d0d7de";
    private const string TextColor = "#24292f";
    private const string MutedTextColor = "#57606a";
    private const string DividerColor = "#eaeef2";
    private const string AlertRedColor = "#cf222e";
    private const string WarningOrangeColor = "#9a6700";

    public static string BuildAccountLockedEmail(string? fullName, string reason)
    {
        var safeName = WebUtility.HtmlEncode(!string.IsNullOrWhiteSpace(fullName) ? fullName.Trim() : "Bạn đọc");
        var safeReason = WebUtility.HtmlEncode(!string.IsNullOrWhiteSpace(reason) ? reason.Trim() : "Vi phạm quy định thư viện");

        var bodyContent = $$"""
        <h1 style="font-size: 20px; font-weight: 600; color: {{AlertRedColor}}; margin: 0 0 16px 0;">Thông báo khóa tài khoản</h1>
        <p style="font-size: 14px; line-height: 1.6; color: {{TextColor}}; margin: 0 0 16px 0;">
          Xin chào <strong>{{safeName}}</strong>,
        </p>
        <p style="font-size: 14px; line-height: 1.6; color: {{TextColor}}; margin: 0 0 20px 0;">
          Tài khoản Thư viện Libri của bạn đã bị tạm khóa bởi hệ thống với lý do sau:
        </p>
        <div style="background-color: #ffebe9; border: 1px solid #ff8182; border-radius: 6px; padding: 16px; margin: 20px 0;">
          <p style="margin: 0; font-size: 14px; color: {{AlertRedColor}}; font-weight: 600;">
            Lý do: {{safeReason}}
          </p>
        </div>
        <p style="font-size: 13px; line-height: 1.5; color: {{MutedTextColor}}; margin: 0 0 24px 0;">
          Nếu bạn cho rằng đây là một sự nhầm lẫn hoặc cần hỗ trợ mở lại tài khoản, vui lòng liên hệ trực tiếp với quầy thủ thư hoặc gửi phản hồi tới ban quản trị thư viện.
        </p>
        <div style="height: 1px; background-color: {{DividerColor}}; margin: 24px 0;"></div>
        <p style="font-size: 14px; line-height: 1.5; color: {{TextColor}}; margin: 0;">
          Trân trọng,<br>
          <strong>Ban Quản trị Thư viện Libri</strong>
        </p>
        """;

        return LibriEmailTemplates.BuildBaseCard("Thông báo khóa tài khoản Libri", bodyContent);
    }

    public static string BuildLoanDueSoonEmail(string? fullName, string bookTitle, DateOnly dueDate)
    {
        var safeName = WebUtility.HtmlEncode(!string.IsNullOrWhiteSpace(fullName) ? fullName.Trim() : "Bạn đọc");
        var safeTitle = WebUtility.HtmlEncode(bookTitle);

        var bodyContent = $$"""
        <h1 style="font-size: 20px; font-weight: 600; color: {{WarningOrangeColor}}; margin: 0 0 16px 0;">Nhắc nhở sắp đến hạn trả sách</h1>
        <p style="font-size: 14px; line-height: 1.6; color: {{TextColor}}; margin: 0 0 16px 0;">
          Xin chào <strong>{{safeName}}</strong>,
        </p>
        <p style="font-size: 14px; line-height: 1.6; color: {{TextColor}}; margin: 0 0 20px 0;">
          Hệ thống Thư viện Libri xin nhắc bạn cuốn sách sau sắp đến hạn trả:
        </p>
        <div style="background-color: {{CanvasBackground}}; border: 1px solid {{BorderColor}}; border-radius: 6px; padding: 16px; margin: 20px 0;">
          <p style="margin: 0 0 8px 0; font-size: 15px; font-weight: 600; color: {{BrandColor}};">
            {{safeTitle}}
          </p>
          <p style="margin: 0; font-size: 13px; color: {{TextColor}};">
            Hạn trả sách: <strong>{{dueDate:dd/MM/yyyy}}</strong>
          </p>
        </div>
        <p style="font-size: 13px; line-height: 1.5; color: {{MutedTextColor}}; margin: 0 0 24px 0;">
          Vui lòng sắp xếp thời gian đến thư viện trả sách đúng hạn hoặc thực hiện gia hạn trực tuyến trên website trước khi đến hạn để tránh phát sinh phí phạt quá hạn.
        </p>
        <div style="height: 1px; background-color: {{DividerColor}}; margin: 24px 0;"></div>
        <p style="font-size: 14px; line-height: 1.5; color: {{TextColor}}; margin: 0;">
          Trân trọng,<br>
          <strong>Đội ngũ Thư viện Libri</strong>
        </p>
        """;

        return LibriEmailTemplates.BuildBaseCard("Nhắc nhở sắp đến hạn trả sách - Libri", bodyContent);
    }

    public static string BuildLoanOverdueEmail(string? fullName, string bookTitle, DateOnly dueDate)
    {
        var safeName = WebUtility.HtmlEncode(!string.IsNullOrWhiteSpace(fullName) ? fullName.Trim() : "Bạn đọc");
        var safeTitle = WebUtility.HtmlEncode(bookTitle);

        var bodyContent = $$"""
        <h1 style="font-size: 20px; font-weight: 600; color: {{AlertRedColor}}; margin: 0 0 16px 0;">Thông báo sách mượn quá hạn</h1>
        <p style="font-size: 14px; line-height: 1.6; color: {{TextColor}}; margin: 0 0 16px 0;">
          Xin chào <strong>{{safeName}}</strong>,
        </p>
        <p style="font-size: 14px; line-height: 1.6; color: {{TextColor}}; margin: 0 0 20px 0;">
          Cuốn sách bạn đang mượn hiện đã quá hạn trả:
        </p>
        <div style="background-color: #ffebe9; border: 1px solid #ff8182; border-radius: 6px; padding: 16px; margin: 20px 0;">
          <p style="margin: 0 0 8px 0; font-size: 15px; font-weight: 600; color: {{AlertRedColor}};">
            {{safeTitle}}
          </p>
          <p style="margin: 0; font-size: 13px; color: {{TextColor}};">
            Hạn trả quy định: <strong>{{dueDate:dd/MM/yyyy}}</strong>
          </p>
        </div>
        <p style="font-size: 13px; line-height: 1.5; color: {{MutedTextColor}}; margin: 0 0 24px 0;">
          Khoản phí phạt quá hạn đang được tính theo từng ngày trễ theo quy định của Thư viện. Bạn vui lòng mang sách đến hoàn trả sớm nhất có thể để giảm thiểu chi phí phát sinh.
        </p>
        <div style="height: 1px; background-color: {{DividerColor}}; margin: 24px 0;"></div>
        <p style="font-size: 14px; line-height: 1.5; color: {{TextColor}}; margin: 0;">
          Trân trọng,<br>
          <strong>Đội ngũ Thư viện Libri</strong>
        </p>
        """;

        return LibriEmailTemplates.BuildBaseCard("Thông báo sách quá hạn trả - Libri", bodyContent);
    }

    public static string BuildReservationCreatedEmail(string? fullName, string bookTitle, bool isReady, DateTime? expiresAt)
    {
        var safeName = WebUtility.HtmlEncode(!string.IsNullOrWhiteSpace(fullName) ? fullName.Trim() : "Bạn đọc");
        var safeTitle = WebUtility.HtmlEncode(bookTitle);

        var statusInfo = isReady
            ? $"Sách hiện đã có sẵn tại thư viện. Hạn giữ sách đến: <strong>{(expiresAt.HasValue ? expiresAt.Value.AddHours(7).ToString("HH:mm dd/MM/yyyy") : "hết hạn theo quy định")}</strong>."
            : "Yêu cầu đặt sách của bạn đã được ghi nhận vào hàng chờ. Khi sách được hoàn trả về thư viện, hệ thống sẽ gửi email thông báo ngay cho bạn.";

        var bodyContent = $$"""
        <h1 style="font-size: 20px; font-weight: 600; color: {{BrandColor}}; margin: 0 0 16px 0;">Xác nhận đặt mượn sách</h1>
        <p style="font-size: 14px; line-height: 1.6; color: {{TextColor}}; margin: 0 0 16px 0;">
          Xin chào <strong>{{safeName}}</strong>,
        </p>
        <p style="font-size: 14px; line-height: 1.6; color: {{TextColor}}; margin: 0 0 20px 0;">
          Bạn vừa đặt giữ trước thành công tựa sách:
        </p>
        <div style="background-color: {{CanvasBackground}}; border: 1px solid {{BorderColor}}; border-radius: 6px; padding: 16px; margin: 20px 0;">
          <p style="margin: 0 0 8px 0; font-size: 15px; font-weight: 600; color: {{BrandColor}};">
            {{safeTitle}}
          </p>
          <p style="margin: 0; font-size: 13px; line-height: 1.5; color: {{TextColor}};">
            {{statusInfo}}
          </p>
        </div>
        <p style="font-size: 13px; line-height: 1.5; color: {{MutedTextColor}}; margin: 0 0 24px 0;">
          Bạn có thể theo dõi trạng thái các sách đã đặt trong mục <em>Đặt trước</em> tại trang tài khoản cá nhân.
        </p>
        <div style="height: 1px; background-color: {{DividerColor}}; margin: 24px 0;"></div>
        <p style="font-size: 14px; line-height: 1.5; color: {{TextColor}}; margin: 0;">
          Trân trọng,<br>
          <strong>Đội ngũ Thư viện Libri</strong>
        </p>
        """;

        return LibriEmailTemplates.BuildBaseCard("Xác nhận đặt giữ sách - Libri", bodyContent);
    }

    public static string BuildReservationReadyEmail(string? fullName, string bookTitle, DateTime? expiresAt)
    {
        var safeName = WebUtility.HtmlEncode(!string.IsNullOrWhiteSpace(fullName) ? fullName.Trim() : "Bạn đọc");
        var safeTitle = WebUtility.HtmlEncode(bookTitle);
        var expiryStr = expiresAt.HasValue
            ? expiresAt.Value.AddHours(7).ToString("HH:mm dd/MM/yyyy")
            : "48 giờ kể từ thông báo này";

        var bodyContent = $$"""
        <h1 style="font-size: 20px; font-weight: 600; color: {{BrandColor}}; margin: 0 0 16px 0;">Sách đặt trước đã sẵn sàng</h1>
        <p style="font-size: 14px; line-height: 1.6; color: {{TextColor}}; margin: 0 0 16px 0;">
          Xin chào <strong>{{safeName}}</strong>,
        </p>
        <p style="font-size: 14px; line-height: 1.6; color: {{TextColor}}; margin: 0 0 20px 0;">
          Cuốn sách bạn đặt giữ trước hiện đã có sẵn tại quầy phục vụ của Thư viện Libri:
        </p>
        <div style="background-color: {{CanvasBackground}}; border: 1px solid {{BorderColor}}; border-radius: 6px; padding: 16px; margin: 20px 0;">
          <p style="margin: 0 0 8px 0; font-size: 15px; font-weight: 600; color: {{BrandColor}};">
            {{safeTitle}}
          </p>
          <p style="margin: 0; font-size: 13px; color: {{TextColor}};">
            Hạn chót đến lấy sách: <strong style="color: {{BrandColor}};">{{expiryStr}}</strong>
          </p>
        </div>
        <p style="font-size: 13px; line-height: 1.5; color: {{MutedTextColor}}; margin: 0 0 24px 0;">
          Vui lòng đến quầy thủ thư xuất trình thẻ bạn đọc/thông tin tài khoản để nhận sách trước thời hạn trên. Sau thời hạn này, yêu cầu giữ sách sẽ tự động bị hủy để nhường cho bạn đọc tiếp theo.
        </p>
        <div style="height: 1px; background-color: {{DividerColor}}; margin: 24px 0;"></div>
        <p style="font-size: 14px; line-height: 1.5; color: {{TextColor}}; margin: 0;">
          Trân trọng,<br>
          <strong>Đội ngũ Thư viện Libri</strong>
        </p>
        """;

        return LibriEmailTemplates.BuildBaseCard("Sách đặt trước đã sẵn sàng nhận - Libri", bodyContent);
    }
}

