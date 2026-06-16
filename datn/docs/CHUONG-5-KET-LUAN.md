# CHƯƠNG 5. KẾT LUẬN

Sau quá trình khảo sát yêu cầu, phân tích thiết kế, cài đặt và kiểm thử hệ thống quản lý mầm non trên nền tảng ASP.NET Core MVC, đề tài đã xây dựng được một ứng dụng web phục vụ ba nhóm người dùng chính: quản trị viên (Manager), giáo viên/nhân viên (Employee) và phụ huynh (Parent). Chương này tổng kết những kết quả đạt được, các hạn chế còn tồn tại trong phiên bản hiện tại và định hướng phát triển trong tương lai.

---

## 5.1. Ưu điểm

**Phạm vi chức năng đầy đủ cho vận hành một trường mầm non**

Hệ thống bao phủ hầu hết các nghiệp vụ cốt lõi: quản lý học sinh, lớp học, phụ huynh, giáo viên; phân công giảng dạy; xây dựng thời khóa biểu theo lớp, môn học và địa điểm; quản lý giáo trình và kế hoạch giảng dạy; điểm danh học sinh; báo cáo ngày (ăn, ngủ, sức khỏe, tâm trạng); theo dõi sức khỏe; báo cáo học tập theo tháng; quản lý hoạt động ngoại khóa; quản lý thực đơn dinh dưỡng; tính học phí theo tháng; chấm công, xin nghỉ và tính lương cho nhân viên. Việc gom các module vào một nền tảng thống nhất giúp ban giám hiệu, giáo viên và phụ huynh thao tác trên cùng một nguồn dữ liệu, hạn chế trùng lặp và sai lệch thông tin.

**Phân quyền rõ ràng theo vai trò**

Ứng dụng triển khai cơ chế xác thực JWT kết hợp refresh token lưu trong HttpOnly cookie, mã hóa mật khẩu bằng BCrypt và phân quyền theo ba vai trò Manager, Employee, Parent. Mỗi vai trò có giao diện dashboard riêng (`Views/Dashboard/Admin`, `Teacher`, `Parent`) với menu và chức năng tương ứng. Giáo viên được phân loại thêm theo `TeacherType` (giáo viên chủ nhiệm / giáo viên bộ môn), phù hợp với mô hình tổ chức thực tế tại trường mầm non.

**Giao diện hiện đại, hỗ trợ nhiều thiết bị**

Giao diện được xây dựng bằng hệ thống CSS tùy chỉnh (`modern-ui.css`, `components.css`) với font Plus Jakarta Sans, hỗ trợ chế độ sáng/tối, sidebar thu gọn và menu hamburger trên màn hình nhỏ (breakpoint 768px). Trang landing giới thiệu trường và các trang đăng nhập cũng được tối ưu cho thiết bị di động. Nhờ đó, giáo viên có thể điểm danh, ghi báo cáo ngày trên điện thoại; phụ huynh có thể xem thông tin con và thanh toán học phí thuận tiện hơn.

**Tích hợp thanh toán học phí trực tuyến**

Phụ huynh có thể tra cứu hóa đơn học phí theo tháng và thanh toán qua ví MoMo (`MoMoService`, `TuitionController`). Hệ thống hỗ trợ cấu hình khoản phí mặc định theo độ tuổi, cấu hình riêng cho từng học sinh, sinh hóa đơn hàng loạt và theo dõi tình trạng thu học phí trên dashboard quản trị. Điều này giảm bớt thao tác thu tiền thủ công và tăng tính minh bạch với phụ huynh.

**Thông báo thời gian thực và nhật ký hệ thống**

Module thông báo kết hợp lưu trữ cơ sở dữ liệu và đẩy tin qua SignalR (`RealtimeHub`, `NotificationService`), giúp người dùng nhận thông tin kịp thời khi có sự kiện như duyệt đơn nghỉ, thanh toán học phí thành công hoặc thay đổi lịch dạy. Đồng thời, `AppDbContext` tự động ghi `AuditLogs` mỗi khi dữ liệu thay đổi, hỗ trợ quản trị viên truy vết thao tác trên hệ thống.

**Xử lý nghiệp vụ nhân sự và giảng dạy có tính thực tiễn**

Hệ thống có quy trình xin nghỉ – duyệt nghỉ – phân công giáo viên dạy thay (`LeaveApprovalController`, `ClassCoverageService`, `Substitutions`), tự động tính lương theo kỳ (`PayrollAutoCalculationService`) và cho phép in phiếu lương (`SalarySlip.cshtml`). Các dashboard của quản trị viên, giáo viên và phụ huynh đều có biểu đồ thống kê (Chart.js, ApexCharts) về doanh thu, điểm danh, xếp loại học tập, giúp ra quyết định nhanh hơn.

**Công nghệ ổn định, dễ bảo trì**

Ứng dụng sử dụng ASP.NET Core 9, Entity Framework Core, SQL Server — bộ công nghệ phổ biến, tài liệu đầy đủ. Kiến trúc monolith với tách lớp Controller – Service – Data phù hợp quy mô đồ án tốt nghiệp, thuận tiện triển khai trên một máy chủ và mở rộng dần khi cần.

---

## 5.2. Hạn chế

**Chưa phù hợp quy mô lớn hoặc nhiều trường**

Hệ thống được thiết kế cho **một trường mầm non** (single-tenant): không có khái niệm `SchoolId` hay `TenantId` trong cơ sở dữ liệu, thương hiệu và cấu hình gắn chặt với một đơn vị. Khi số lượng học sinh, giáo viên và lớp tăng lên đáng kể, một số API vẫn trả về toàn bộ danh sách mà chưa phân trang phía server, có thể ảnh hưởng hiệu năng. Chưa có cơ chế triển khai đa trường trên cùng một hệ thống.

**Responsive chưa đồng đều trên toàn bộ giao diện**

Mặc dù layout chính, trang landing và nhiều màn hình đã có media query, một số bảng dữ liệu phức tạp ở khu vực quản trị (quản lý TKB, học phí, nhật ký hệ thống) vẫn hiển thị nhiều cột, khó thao tác trên màn hình điện thoại nhỏ. Trải nghiệm trên tablet và desktop tốt hơn so với mobile đối với các form quản trị nặng.

**Thống kê và báo cáo còn ở mức cơ bản**

Dashboard hiện cung cấp số liệu tổng hợp (doanh thu theo tháng, điểm danh trong ngày, phân bố xếp loại…), nhưng **chưa có module báo cáo chuyên sâu** như: thống kê theo ca làm việc của giáo viên, theo từng môn học, theo từng lớp trong khoảng thời gian tùy chọn, so sánh giữa các học kỳ. Chưa hỗ trợ xuất báo cáo ra file Excel, PDF hay in trực tiếp từ trình duyệt (trừ phiếu lương).

**Chức năng in ấn còn hạn chế**

Hệ thống mới chỉ hỗ trợ **in phiếu lương** cho giáo viên (`window.print()` trên `SalarySlip.cshtml`). **Chưa có chức năng in hóa đơn học phí**, biên lai thu tiền, báo cáo điểm danh hay báo cáo học tập ra giấy qua máy in — trong khi nhu cầu này vẫn phổ biến tại các trường khi làm việc với phụ huynh và cơ quan quản lý.

**Triển khai production chưa hoàn thiện**

Hiện tại ứng dụng chủ yếu chạy trên môi trường phát triển cục bộ (SQL Server Express, `localhost`). Trong mã nguồn **chưa có** cấu hình Docker, `appsettings.Production.json`, pipeline CI/CD hay hướng dẫn deploy lên máy chủ thực tế. Thông tin nhạy cảm (JWT secret, khóa MoMo, mật khẩu email) đang để trong `appsettings.json`, chưa tách biệt theo môi trường. Việc chưa đưa lên server tập trung làm tăng rủi ro mất dữ liệu khi máy cá nhân hỏng và hạn chế truy cập đồng thời từ nhiều thiết bị.

**Phương thức thanh toán và một số điểm kỹ thuật**

Hệ thống mới tích hợp **MoMo**; chưa hỗ trợ chuyển khoản ngân hàng, tiền mặt có xác nhận trên hệ thống hay các cổng thanh toán khác. Luồng webhook IPN cần được kiểm chứng kỹ hơn trên môi trường thật. Ảnh đại diện lưu trên `wwwroot` cục bộ, chưa dùng lưu trữ đám mây — khó mở rộng khi triển khai nhiều instance.

---

## 5.3. Đề xuất giải pháp

**Mở rộng quy mô và mô hình triển khai**

Nghiên cứu kiến trúc **đa trường (multi-tenant)** hoặc triển khai riêng từng instance cho mỗi trường, bổ sung phân trang phía server cho các danh sách lớn (học sinh, hóa đơn, nhật ký). Cân nhắc cache (Redis), tối ưu truy vấn và tách background job (tính lương, gửi email) sang hàng đợi khi lượng người dùng tăng.

**Hoàn thiện responsive và trải nghiệm người dùng**

Rà soát toàn bộ view quản trị, chuyển các bảng rộng sang dạng card/stack trên mobile, thêm cuộn ngang có nhãn hoặc chế độ xem rút gọn. Tiếp tục tinh chỉnh giao diện theo phản hồi người dùng thực tế (màu sắc, khoảng cách, thứ tự thao tác) để giảm thời gian làm quen, đặc biệt với giáo viên ít rành công nghệ.

**Bổ sung báo cáo và xuất dữ liệu**

Xây dựng module báo cáo cho phép lọc theo **lớp, môn, giáo viên, ca làm việc, khoảng thời gian**; thống kê điểm danh, học phí, chấm công, dinh dưỡng. Hỗ trợ **xuất Excel/PDF** và **in hóa đơn học phí, biên lai** qua máy in (sử dụng CSS `@media print` hoặc thư viện tạo PDF như QuestPDF).

**Triển khai lên máy chủ production**

Đưa ứng dụng lên VPS hoặc cloud (Azure, AWS, Viettel IDC…), cấu hình HTTPS, sao lưu cơ sở dữ liệu định kỳ, dùng biến môi trường cho secret thay vì hard-code. Bổ sung `Dockerfile`, `appsettings.Production.json` và quy trình deploy tự động để đảm bảo dữ liệu an toàn và người dùng truy cập ổn định mọi lúc.

**Mở rộng kênh thanh toán và thông báo**

Bổ sung ghi nhận thanh toán tiền mặt/chuyển khoản do quản trị xác nhận, tích hợp thêm cổng thanh toán phổ biến. Cân nhắc gửi thông báo qua email/SMS/Zalo OA bên cạnh SignalR để phụ huynh không bỏ lỡ thông tin khi không mở ứng dụng.

**Nâng cao bảo mật và khả năng mở rộng**

Áp dụng rate limiting, CSP header, xoay khóa JWT định kỳ; lưu file upload trên cloud storage; dùng Azure SignalR Service hoặc Redis backplane nếu triển khai nhiều node. Có thể phát triển thêm **ứng dụng di động** hoặc **PWA** để giáo viên và phụ huynh sử dụng thuận tiện hơn trên điện thoại.

---

## Tóm tắt

Đề tài đã hoàn thành một hệ thống quản lý mầm non có phạm vi chức năng rộng, kiến trúc rõ ràng và giao diện thân thiện, đáp ứng tốt nhu cầu vận hành của một trường ở quy mô vừa và nhỏ. Các hạn chế chủ yếu nằm ở khả năng mở rộng quy mô, báo cáo chuyên sâu, in ấn chứng từ và triển khai production — đây cũng là hướng phát triển tự nhiên nếu đưa sản phẩm từ môi trường đồ án sang vận hành thực tế lâu dài.
