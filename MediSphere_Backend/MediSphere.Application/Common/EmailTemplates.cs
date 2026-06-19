using System;
using System.Text;

namespace MediSphere.Application.Common;

public static class EmailTemplates
{
    public static string BuildHtmlTemplate(string title, string welcomeText, string contentHtml,
        string? actionUrl = null, string? actionText = null)
    {
        var sb = new StringBuilder();
        sb.Append("<!DOCTYPE html><html><head><meta charset='utf-8'>");
        sb.Append("<style>");
        sb.Append("body { font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; background-color: #f4f6f9; color: #333; margin: 0; padding: 20px; }");
        sb.Append(".container { max-width: 600px; margin: 0 auto; background-color: #ffffff; border-radius: 8px; box-shadow: 0 4px 12px rgba(0,0,0,0.08); overflow: hidden; border: 1px solid #eef2f5; }");
        sb.Append(".header { background: linear-gradient(135deg, #0f172a 0%, #1e293b 100%); color: #ffffff; padding: 32px 30px; text-align: center; }");
        sb.Append(".header h1 { margin: 0; font-size: 24px; font-weight: 600; letter-spacing: 0.5px; }");
        sb.Append(".content { padding: 40px 36px; line-height: 1.8; font-size: 15px; }");
        sb.Append(".content h2 { color: #0f172a; margin-top: 0; font-size: 20px; margin-bottom: 20px; }");
        sb.Append(".content p { margin: 0 0 16px 0; }");
        sb.Append(".info-box { background: #f0f9ff; border: 1px solid #bae6fd; border-radius: 10px; padding: 0; margin: 24px 0; overflow: hidden; }");
        sb.Append(".info-table { width: 100%; border-collapse: collapse; }");
        sb.Append(".info-table tr { border-bottom: 1px solid #daeefb; }");
        sb.Append(".info-table tr:last-child { border-bottom: none; }");
        sb.Append(".info-table td { padding: 13px 20px; font-size: 14.5px; vertical-align: middle; }");
        sb.Append(".info-label { color: #64748b; font-weight: 500; white-space: nowrap; padding-right: 4px; }");
        sb.Append(".info-colon { color: #94a3b8; padding-left: 0; padding-right: 12px; }");
        sb.Append(".info-value { color: #0f172a; font-weight: 600; }");
        sb.Append(".badge { display: inline-block; padding: 5px 14px; border-radius: 20px; font-size: 12px; font-weight: 700; }");
        sb.Append(".badge-success { background: #dcfce7; color: #15803d; }");
        sb.Append(".badge-warning { background: #fef9c3; color: #b45309; }");
        sb.Append(".badge-danger  { background: #fee2e2; color: #b91c1c; }");
        sb.Append(".badge-info    { background: #e0f2fe; color: #0369a1; }");
        sb.Append(".btn { display: inline-block; padding: 13px 28px; background-color: #3b82f6; color: #ffffff !important; text-decoration: none; border-radius: 6px; font-weight: bold; margin-top: 24px; text-align: center; font-size: 15px; }");
        sb.Append(".btn-success { background-color: #16a34a; }");
        sb.Append(".btn-danger  { background-color: #dc2626; }");
        sb.Append(".footer { background-color: #f8fafc; padding: 24px; text-align: center; font-size: 12px; color: #64748b; border-top: 1px solid #e2e8f0; line-height: 1.7; }");
        sb.Append("</style></head><body>");
        sb.Append("<div class='container'>");
        sb.Append($"<div class='header'><h1>🏥 {title}</h1></div>");
        sb.Append("<div class='content'>");
        sb.Append($"<h2>{welcomeText}</h2>");
        sb.Append(contentHtml);
        if (!string.IsNullOrEmpty(actionUrl) && !string.IsNullOrEmpty(actionText))
            sb.Append($"<div style='text-align: center;'><a href='{actionUrl}' class='btn'>{actionText}</a></div>");
        sb.Append("</div>");
        sb.Append("<div class='footer'><p>&copy; 2026 MediSphere Healthcare Platform. All rights reserved.</p><p>This is an automated operational notification. Please do not reply directly.</p></div>");
        sb.Append("</div></body></html>");
        return sb.ToString();
    }

    // ─────────────────────────────────────────────────────────────
    // PATIENT EMAILS
    // ─────────────────────────────────────────────────────────────

    public static string BuildPatientRegistrationSuccessEmail(string patientName, string referralCode)
    {
        var content = $@"
            <p>Hello <strong>{patientName}</strong>,</p>
            <p>Welcome to MediSphere! Your patient account has been successfully created.</p>
            <p>You can now search for premium doctors, book consultations, upload medical history, and earn reward points.</p>
            <div class='info-box'>
                <table class='info-table'>
                    <tr><td class='info-label'>Your Referral Code</td><td class='info-colon'>:</td><td class='info-value'>{referralCode}</td></tr>
                </table>
            </div>
            <p>Share your referral code with friends and family. When they register and make their first booking, both of you earn reward points — 100 points for them, 50 points for you — redeemable for up to 50% off on consultation fees!</p>
            <p>Thank you for choosing MediSphere! &#128153;</p>";

        return BuildHtmlTemplate("Welcome to MediSphere", "Registration Successful!", content,
            "https://medisphere.app/dashboard", "Go to Dashboard");
    }

    public static string BuildPasswordResetEmail(string userName, string otp, int expiryMinutes)
    {
        var content = $@"
        <p>Hello <strong>{userName}</strong>,</p>
        <p>We received a request to reset your MediSphere account password.</p>
        <p>Please use the One-Time Password (OTP) below to continue:</p>
        <div style='background:#f8fafc;border:1px solid #e2e8f0;border-radius:12px;padding:28px 24px;margin:24px 0;text-align:center;'>
            <div style='color:#64748b;font-size:14px;margin-bottom:10px;font-weight:500;'>PASSWORD RESET OTP</div>
            <div style='font-size:42px;font-weight:700;letter-spacing:10px;color:#2563eb;margin:10px 0;'>{otp}</div>
            <div style='color:#64748b;font-size:13px;'>Valid for {expiryMinutes} minutes</div>
        </div>
        <div class='info-box'>
            <table class='info-table'>
                <tr><td colspan='3' style='padding:14px 20px;font-size:14px;color:#0f172a;'>
                    <strong>&#128274; Security Notice:</strong> Never share this OTP with anyone, including MediSphere staff.
                </td></tr>
            </table>
        </div>
        <p>Enter this OTP on the password reset page to create a new password.</p>
        <p>If you did not request a password reset, you can safely ignore this email.</p>";

        return BuildHtmlTemplate("Password Reset", "Verify Your Identity", content);
    }

    public static string BuildReferralBonusEmail(
        string recipientName, int pointsEarned, int totalPoints,
        string referredPatientName, bool isReferrer)
    {
        var headline = isReferrer
            ? $"You Earned {pointsEarned} Points for Referring {referredPatientName}!"
            : $"Welcome Bonus of {pointsEarned} Points Credited!";

        var content = $@"
            <p>Hello <strong>{recipientName}</strong>,</p>
            <p>Great news — you have earned <strong>{pointsEarned} reward points</strong>!</p>
            <div class='info-box'>
                <table class='info-table'>
                    <tr><td class='info-label'>Points Earned</td><td class='info-colon'>:</td><td class='info-value'>+{pointsEarned} pts</td></tr>
                    <tr><td class='info-label'>{(isReferrer ? "Referred Patient" : "Referral Source")}</td><td class='info-colon'>:</td><td class='info-value'>{referredPatientName}</td></tr>
                    <tr><td class='info-label'>Total Balance</td><td class='info-colon'>:</td><td class='info-value'>{totalPoints} pts (&#8776; &#8377;{totalPoints})</td></tr>
                </table>
            </div>
            <p>Use your points for up to <strong>50% discount</strong> on your next consultation fee at checkout!</p>
            <p>Keep referring friends and earning more points. &#127775;</p>";

        return BuildHtmlTemplate("Reward Points Credited", headline, content,
            "https://medisphere.app/dashboard", "View My Rewards");
    }

    // ─────────────────────────────────────────────────────────────
    // APPOINTMENT EMAILS — PATIENT
    // ─────────────────────────────────────────────────────────────

    public static string BuildAppointmentConfirmationEmail(
        string patientName, string doctorName, string specialty,
        string date, string time, int queueToken, decimal fee, string meetingUrl)
    {
        var content = $@"
            <p>Hello <strong>{patientName}</strong>,</p>
            <p>Your appointment has been successfully booked. Here are the details:</p>
            <div class='info-box'>
                <table class='info-table'>
                    <tr><td class='info-label'>Doctor</td><td class='info-colon'>:</td><td class='info-value'>Dr. {doctorName} ({specialty})</td></tr>
                    <tr><td class='info-label'>Date</td><td class='info-colon'>:</td><td class='info-value'>{date}</td></tr>
                    <tr><td class='info-label'>Time</td><td class='info-colon'>:</td><td class='info-value'>{time}</td></tr>
                    <tr><td class='info-label'>Queue Token</td><td class='info-colon'>:</td><td class='info-value'>#{queueToken}</td></tr>
                    <tr><td class='info-label'>Consultation Fee</td><td class='info-colon'>:</td><td class='info-value'>&#8377;{fee:N2}</td></tr>
                    <tr><td class='info-label'>Status</td><td class='info-colon'>:</td><td class='info-value'><span class='badge badge-success'>Confirmed</span></td></tr>
                </table>
            </div>
            {(string.IsNullOrEmpty(meetingUrl) ? "" : $"<p>&#127909; <strong>Telemedicine Link:</strong> <a href='{meetingUrl}'>{meetingUrl}</a></p>")}
            <p>Please arrive 10 minutes early or keep the telemedicine link handy if it's a virtual consultation.</p>
            <p>Thank you for choosing MediSphere for your healthcare needs! &#128153;</p>";

        return BuildHtmlTemplate("Appointment Confirmed", "Your Appointment is Booked!", content,
            "https://medisphere.app/appointments/history", "View My Appointments");
    }

    public static string BuildAppointmentReminderEmail(
        string patientName, string doctorName, string specialty,
        string date, string time, int queueToken, string? meetingUrl)
    {
        var content = $@"
            <p>Hello <strong>{patientName}</strong>,</p>
            <p>&#9200; This is a friendly reminder that your appointment is coming up in <strong>2 hours</strong>!</p>
            <div class='info-box'>
                <table class='info-table'>
                    <tr><td class='info-label'>Doctor</td><td class='info-colon'>:</td><td class='info-value'>Dr. {doctorName} ({specialty})</td></tr>
                    <tr><td class='info-label'>Date</td><td class='info-colon'>:</td><td class='info-value'>{date}</td></tr>
                    <tr><td class='info-label'>Time</td><td class='info-colon'>:</td><td class='info-value'>{time}</td></tr>
                    <tr><td class='info-label'>Queue Token</td><td class='info-colon'>:</td><td class='info-value'>#{queueToken}</td></tr>
                </table>
            </div>
            {(string.IsNullOrEmpty(meetingUrl)
                ? "<p>&#128205; Please arrive at the clinic 10 minutes before your scheduled time.</p>"
                : $"<p>&#127909; <strong>Join your telemedicine call:</strong> <a href='{meetingUrl}'>{meetingUrl}</a></p>")}
            <p>Prepare your medical history, previous prescriptions, and any relevant documents.</p>";

        return BuildHtmlTemplate("Appointment Reminder", "Your Appointment is in 2 Hours!", content,
            meetingUrl ?? "https://medisphere.app/appointments/history",
            meetingUrl != null ? "Join Telemedicine Call" : "View Appointment");
    }

    public static string BuildAppointmentCompletedEmail(
        string patientName, string doctorName, string specialty,
        string date, string time, decimal fee)
    {
        var content = $@"
            <p>Hello <strong>{patientName}</strong>,</p>
            <p>Your consultation with <strong>Dr. {doctorName}</strong> has been marked as <strong>completed</strong>. We hope your visit went well!</p>
            <div class='info-box'>
                <table class='info-table'>
                    <tr><td class='info-label'>Doctor</td><td class='info-colon'>:</td><td class='info-value'>Dr. {doctorName} ({specialty})</td></tr>
                    <tr><td class='info-label'>Date</td><td class='info-colon'>:</td><td class='info-value'>{date}</td></tr>
                    <tr><td class='info-label'>Time</td><td class='info-colon'>:</td><td class='info-value'>{time}</td></tr>
                    <tr><td class='info-label'>Consultation Fee</td><td class='info-colon'>:</td><td class='info-value'>&#8377;{fee:N2}</td></tr>
                    <tr><td class='info-label'>Status</td><td class='info-colon'>:</td><td class='info-value'><span class='badge badge-success'>Completed</span></td></tr>
                </table>
            </div>
            <p>We'd love to hear about your experience. Please take a moment to leave a review for Dr. {doctorName} — it helps other patients make informed decisions.</p>
            <p>Thank you for choosing MediSphere for your healthcare needs! &#128153;</p>";

        return BuildHtmlTemplate("Consultation Completed", "Your Appointment is Complete!", content,
            "https://medisphere.app/appointments/history", "Leave a Review");
    }

    public static string BuildAppointmentCancelledEmail(
        string patientName, string doctorName, string date, string time, string? reason)
    {
        var content = $@"
            <p>Hello <strong>{patientName}</strong>,</p>
            <p>Your appointment has been <strong>cancelled</strong>.</p>
            <div class='info-box'>
                <table class='info-table'>
                    <tr><td class='info-label'>Doctor</td><td class='info-colon'>:</td><td class='info-value'>Dr. {doctorName}</td></tr>
                    <tr><td class='info-label'>Date</td><td class='info-colon'>:</td><td class='info-value'>{date}</td></tr>
                    <tr><td class='info-label'>Time</td><td class='info-colon'>:</td><td class='info-value'>{time}</td></tr>
                    {(string.IsNullOrEmpty(reason) ? "" : $"<tr><td class='info-label'>Reason</td><td class='info-colon'>:</td><td class='info-value'>{reason}</td></tr>")}
                    <tr><td class='info-label'>Status</td><td class='info-colon'>:</td><td class='info-value'><span class='badge badge-danger'>Cancelled</span></td></tr>
                </table>
            </div>
            <p>If a payment was made, a refund will be processed within 5-7 business days.</p>
            <p>You can book a new appointment at any time from your dashboard.</p>";

        return BuildHtmlTemplate("Appointment Cancelled", "Your Appointment Has Been Cancelled", content,
            "https://medisphere.app/doctors", "Book a New Appointment");
    }

    public static string BuildAppointmentRejectedEmail(
        string patientName, string doctorName, string date, string time, string? reason)
    {
        var content = $@"
            <p>Hello <strong>{patientName}</strong>,</p>
            <p>Unfortunately, your appointment request with <strong>Dr. {doctorName}</strong> has been <strong>rejected</strong>.</p>
            <div class='info-box'>
                <table class='info-table'>
                    <tr><td class='info-label'>Doctor</td><td class='info-colon'>:</td><td class='info-value'>Dr. {doctorName}</td></tr>
                    <tr><td class='info-label'>Date</td><td class='info-colon'>:</td><td class='info-value'>{date}</td></tr>
                    <tr><td class='info-label'>Time</td><td class='info-colon'>:</td><td class='info-value'>{time}</td></tr>
                    {(string.IsNullOrEmpty(reason) ? "" : $"<tr><td class='info-label'>Reason</td><td class='info-colon'>:</td><td class='info-value'>{reason}</td></tr>")}
                    <tr><td class='info-label'>Status</td><td class='info-colon'>:</td><td class='info-value'><span class='badge badge-danger'>Rejected</span></td></tr>
                </table>
            </div>
            <p>If a payment was made, a full refund will be processed within 5-7 business days.</p>
            <p>You're welcome to book an appointment with another available doctor on MediSphere.</p>";

        return BuildHtmlTemplate("Appointment Rejected", "Your Appointment Request Was Not Accepted", content,
            "https://medisphere.app/doctors", "Find Another Doctor");
    }

    public static string BuildPatientNoShowEmail(
        string patientName, string doctorName, string date, string time)
    {
        var content = $@"
            <p>Hello <strong>{patientName}</strong>,</p>
            <p>We noticed that you did not attend your scheduled appointment. We hope everything is alright!</p>
            <div class='info-box'>
                <table class='info-table'>
                    <tr><td class='info-label'>Doctor</td><td class='info-colon'>:</td><td class='info-value'>Dr. {doctorName}</td></tr>
                    <tr><td class='info-label'>Date</td><td class='info-colon'>:</td><td class='info-value'>{date}</td></tr>
                    <tr><td class='info-label'>Time</td><td class='info-colon'>:</td><td class='info-value'>{time}</td></tr>
                    <tr><td class='info-label'>Status</td><td class='info-colon'>:</td><td class='info-value'><span class='badge badge-warning'>No Show</span></td></tr>
                </table>
            </div>
            <p>Please note that repeated no-shows may affect your ability to book future appointments on the platform.</p>
            <p>If you need to reschedule, you can book a new appointment at any time from your dashboard.</p>
            <p>If you believe this was marked incorrectly, please contact us at <a href='mailto:support@medisphere.com'>support@medisphere.com</a></p>";

        return BuildHtmlTemplate("Missed Appointment", "You Missed Your Appointment", content,
            "https://medisphere.app/doctors", "Book a New Appointment");
    }

    // ─────────────────────────────────────────────────────────────
    // PAYMENT EMAILS — PATIENT
    // ─────────────────────────────────────────────────────────────

    public static string BuildPaymentCapturedEmail(
        string patientName, string doctorName, decimal amount, decimal adminCommission,
        decimal doctorEarnings, decimal taxAmount, string paymentId, string date, string time)
    {
        var content = $@"
            <p>Hello <strong>{patientName}</strong>,</p>
            <p>Your payment has been successfully captured and your appointment is now <strong>confirmed</strong>.</p>
            <div class='info-box'>
                <table class='info-table'>
                    <tr><td class='info-label'>Payment ID</td><td class='info-colon'>:</td><td class='info-value'>{paymentId}</td></tr>
                    <tr><td class='info-label'>Doctor</td><td class='info-colon'>:</td><td class='info-value'>Dr. {doctorName}</td></tr>
                    <tr><td class='info-label'>Date &amp; Time</td><td class='info-colon'>:</td><td class='info-value'>{date} at {time}</td></tr>
                    <tr><td class='info-label'>Amount Paid</td><td class='info-colon'>:</td><td class='info-value'>&#8377;{amount:N2}</td></tr>
                    <tr><td class='info-label'>Tax (GST)</td><td class='info-colon'>:</td><td class='info-value'>&#8377;{taxAmount:N2}</td></tr>
                    <tr><td class='info-label'>Status</td><td class='info-colon'>:</td><td class='info-value'><span class='badge badge-success'>PAID</span></td></tr>
                </table>
            </div>
            <p>A receipt has been recorded in your dashboard. You may print it for insurance or reimbursement purposes.</p>
            <p>Thank you for trusting MediSphere! &#128153;</p>";

        return BuildHtmlTemplate("Payment Receipt", "Payment Successful!", content,
            "https://medisphere.app/appointments/history", "View Receipt");
    }

    public static string BuildPaymentFailedEmail(
        string patientName, string doctorName, decimal amount, string orderId)
    {
        var content = $@"
            <p>Hello <strong>{patientName}</strong>,</p>
            <p>Unfortunately, your payment attempt for your appointment with <strong>Dr. {doctorName}</strong> has failed.</p>
            <div class='info-box'>
                <table class='info-table'>
                    <tr><td class='info-label'>Razorpay Order ID</td><td class='info-colon'>:</td><td class='info-value'>{orderId}</td></tr>
                    <tr><td class='info-label'>Amount</td><td class='info-colon'>:</td><td class='info-value'>&#8377;{amount:N2}</td></tr>
                    <tr><td class='info-label'>Status</td><td class='info-colon'>:</td><td class='info-value'><span class='badge badge-danger'>FAILED</span></td></tr>
                </table>
            </div>
            <p>Your appointment is currently in <strong>Pending Payment</strong> status. You can retry the payment from your dashboard.</p>
            <p>If the amount was debited from your account, it will be refunded within 5-7 business days.</p>
            <p>Need help? Contact us at <a href='mailto:support@medisphere.com'>support@medisphere.com</a></p>";

        return BuildHtmlTemplate("Payment Failed", "Payment Could Not Be Processed", content,
            "https://medisphere.app/appointments/history", "Retry Payment");
    }

    // ─────────────────────────────────────────────────────────────
    // DOCTOR EMAILS
    // ─────────────────────────────────────────────────────────────

    public static string BuildDoctorRegistrationReceivedEmail(
        string doctorName, string specialty, string licenseNumber)
    {
        var content = $@"
            <p>Hello <strong>Dr. {doctorName}</strong>,</p>
            <p>Thank you for registering on MediSphere! We have received your application and it is currently under review.</p>
            <div class='info-box'>
                <table class='info-table'>
                    <tr><td class='info-label'>Name</td><td class='info-colon'>:</td><td class='info-value'>Dr. {doctorName}</td></tr>
                    <tr><td class='info-label'>Specialty</td><td class='info-colon'>:</td><td class='info-value'>{specialty}</td></tr>
                    <tr><td class='info-label'>License No.</td><td class='info-colon'>:</td><td class='info-value'>{licenseNumber}</td></tr>
                    <tr><td class='info-label'>Status</td><td class='info-colon'>:</td><td class='info-value'><span class='badge badge-warning'>Pending Review</span></td></tr>
                </table>
            </div>
            <p>Our admin team will review your credentials and notify you within 2-3 business days.</p>
            <p>You will not be able to accept appointments until your profile is approved.</p>";

        return BuildHtmlTemplate("Registration Received", "Your Application is Under Review", content);
    }

    public static string BuildDoctorApprovedEmail(string doctorName, string specialty)
    {
        var content = $@"
            <p>Hello <strong>Dr. {doctorName}</strong>,</p>
            <p>Congratulations! Your MediSphere doctor profile has been <strong>approved</strong>.</p>
            <div class='info-box'>
                <table class='info-table'>
                    <tr><td class='info-label'>Specialty</td><td class='info-colon'>:</td><td class='info-value'>{specialty}</td></tr>
                    <tr><td class='info-label'>Status</td><td class='info-colon'>:</td><td class='info-value'><span class='badge badge-success'>Approved</span></td></tr>
                </table>
            </div>
            <p>Your profile is now <strong>live</strong> and patients can book appointments with you.</p>
            <p>Log into your doctor dashboard to set your availability, manage appointments, and track your earnings.</p>
            <p>Welcome to the MediSphere family! &#128153;</p>";

        return BuildHtmlTemplate("Profile Approved", "Congratulations — You're Live!", content,
            "https://medisphere.app/doctor-dashboard", "Go to Doctor Dashboard");
    }

    public static string BuildDoctorRejectedEmail(string doctorName, string reason)
    {
        var content = $@"
            <p>Hello <strong>Dr. {doctorName}</strong>,</p>
            <p>We regret to inform you that your MediSphere doctor registration has not been approved at this time.</p>
            <div class='info-box'>
                <table class='info-table'>
                    <tr><td class='info-label'>Status</td><td class='info-colon'>:</td><td class='info-value'><span class='badge badge-danger'>Rejected</span></td></tr>
                    <tr><td class='info-label'>Reason</td><td class='info-colon'>:</td><td class='info-value'>{reason}</td></tr>
                </table>
            </div>
            <p>You may re-apply after addressing the above concerns. Please ensure all documents are valid and up to date.</p>
            <p>If you believe this decision was made in error, please contact us at <a href='mailto:support@medisphere.com'>support@medisphere.com</a></p>";

        return BuildHtmlTemplate("Registration Rejected", "Your Application Was Not Approved", content);
    }

    public static string BuildDoctorSuspendedEmail(string doctorName, string reason, bool isPermanent)
    {
        var statusLabel = isPermanent ? "Blocked (Permanent)" : "Suspended (Temporary)";
        var badgeClass = isPermanent ? "badge-danger" : "badge-warning";
        var content = $@"
            <p>Hello <strong>Dr. {doctorName}</strong>,</p>
            <p>Your MediSphere account access has been restricted by our admin team.</p>
            <div class='info-box'>
                <table class='info-table'>
                    <tr><td class='info-label'>Status</td><td class='info-colon'>:</td><td class='info-value'><span class='badge {badgeClass}'>{statusLabel}</span></td></tr>
                    <tr><td class='info-label'>Reason</td><td class='info-colon'>:</td><td class='info-value'>{reason}</td></tr>
                </table>
            </div>
            <p>During this period, your profile will not be visible to patients and no new appointments can be booked.</p>
            {(isPermanent
                ? "<p>This action is permanent. If you wish to appeal, contact <a href='mailto:legal@medisphere.com'>legal@medisphere.com</a>.</p>"
                : "<p>This is a temporary suspension. You will be notified when your account is restored.</p>")}
            <p>For queries, contact <a href='mailto:support@medisphere.com'>support@medisphere.com</a></p>";

        return BuildHtmlTemplate("Account Status Update", $"Your Account Has Been {statusLabel}", content);
    }

    public static string BuildDoctorUnblockedEmail(string doctorName)
    {
        var content = $@"
            <p>Hello <strong>Dr. {doctorName}</strong>,</p>
            <p>We are pleased to inform you that your MediSphere account has been <strong>reinstated</strong>.</p>
            <div class='info-box'>
                <table class='info-table'>
                    <tr><td class='info-label'>Status</td><td class='info-colon'>:</td><td class='info-value'><span class='badge badge-success'>Active</span></td></tr>
                </table>
            </div>
            <p>Your profile is now visible to patients and you can start accepting appointments again.</p>
            <p>If you have any questions, feel free to reach out at <a href='mailto:support@medisphere.com'>support@medisphere.com</a></p>
            <p>Welcome back to MediSphere! &#128153;</p>";

        return BuildHtmlTemplate("Account Reinstated", "Your Account Has Been Unblocked", content,
            "https://medisphere.app/doctor-dashboard", "Go to Doctor Dashboard");
    }

    public static string BuildDoctorNewAppointmentEmail(
        string doctorName, string patientName, string date, string time,
        int queueToken, string reason, decimal fee)
    {
        var content = $@"
            <p>Hello <strong>Dr. {doctorName}</strong>,</p>
            <p>A new appointment has been booked for you.</p>
            <div class='info-box'>
                <table class='info-table'>
                    <tr><td class='info-label'>Patient</td><td class='info-colon'>:</td><td class='info-value'>{patientName}</td></tr>
                    <tr><td class='info-label'>Date</td><td class='info-colon'>:</td><td class='info-value'>{date}</td></tr>
                    <tr><td class='info-label'>Time</td><td class='info-colon'>:</td><td class='info-value'>{time}</td></tr>
                    <tr><td class='info-label'>Queue Token</td><td class='info-colon'>:</td><td class='info-value'>#{queueToken}</td></tr>
                    <tr><td class='info-label'>Reason</td><td class='info-colon'>:</td><td class='info-value'>{reason}</td></tr>
                    <tr><td class='info-label'>Consultation Fee</td><td class='info-colon'>:</td><td class='info-value'>&#8377;{fee:N2}</td></tr>
                </table>
            </div>
            <p>Please review the patient history and prepare accordingly. You can view full details in your doctor dashboard.</p>";

        return BuildHtmlTemplate("New Appointment", "New Appointment Booked!", content,
            "https://medisphere.app/doctor-dashboard", "View Dashboard");
    }

    public static string BuildDoctorNoShowAlertEmail(
        string doctorName, string patientName, string date, string time)
    {
        var content = $@"
            <p>Hello <strong>Dr. {doctorName}</strong>,</p>
            <p>We want to inform you that the patient for the following appointment did not show up.</p>
            <div class='info-box'>
                <table class='info-table'>
                    <tr><td class='info-label'>Patient</td><td class='info-colon'>:</td><td class='info-value'>{patientName}</td></tr>
                    <tr><td class='info-label'>Date</td><td class='info-colon'>:</td><td class='info-value'>{date}</td></tr>
                    <tr><td class='info-label'>Time</td><td class='info-colon'>:</td><td class='info-value'>{time}</td></tr>
                    <tr><td class='info-label'>Status</td><td class='info-colon'>:</td><td class='info-value'><span class='badge badge-warning'>No Show</span></td></tr>
                </table>
            </div>
            <p>This slot has been marked as a no-show in the system. Your schedule has been updated accordingly.</p>
            <p>If you have any concerns, please contact us at <a href='mailto:support@medisphere.com'>support@medisphere.com</a></p>";

        return BuildHtmlTemplate("Patient No Show", "Patient Did Not Attend", content,
            "https://medisphere.app/doctor-dashboard", "View Dashboard");
    }

    public static string BuildDoctorNewReviewEmail(
        string doctorName, string patientName, int rating, string comment, string date)
    {
        var content = $@"
            <p>Hello <strong>Dr. {doctorName}</strong>,</p>
            <p>A patient has submitted a review for your consultation.</p>
            <div class='info-box'>
                <table class='info-table'>
                    <tr><td class='info-label'>Patient</td><td class='info-colon'>:</td><td class='info-value'>{patientName}</td></tr>
                    <tr><td class='info-label'>Rating</td><td class='info-colon'>:</td><td class='info-value'>{rating} / 5 Stars</td></tr>
                    <tr><td class='info-label'>Review</td><td class='info-colon'>:</td><td class='info-value'>{comment}</td></tr>
                    <tr><td class='info-label'>Date</td><td class='info-colon'>:</td><td class='info-value'>{date}</td></tr>
                </table>
            </div>
            <p>Patient feedback helps improve healthcare quality. Thank you for your excellent service!</p>";

        return BuildHtmlTemplate("New Patient Review", "You Have a New Review!", content,
            "https://medisphere.app/doctor-dashboard", "View Dashboard");
    }

    public static string BuildDoctorEarningsUpdateEmail(string doctorName, decimal amount, decimal totalEarnings)
    {
        var content = $@"
            <p>Hello <strong>Dr. {doctorName}</strong>,</p>
            <p>Your earnings have been updated for a completed consultation.</p>
            <div class='info-box'>
                <table class='info-table'>
                    <tr><td class='info-label'>Consultation Earnings Added</td><td class='info-colon'>:</td><td class='info-value'>&#8377;{amount:N2}</td></tr>
                    <tr><td class='info-label'>Total Net Balance</td><td class='info-colon'>:</td><td class='info-value'>&#8377;{totalEarnings:N2}</td></tr>
                </table>
            </div>
            <p>Log in to your clinician dashboard to view complete transaction histories, payouts, and monthly reports.</p>";

        return BuildHtmlTemplate("Earnings Updated", "New Consultation Earnings Credited", content,
            "https://medisphere.app/doctor-dashboard", "View Earnings Panel");
    }

    // ─────────────────────────────────────────────────────────────
    // ADMIN EMAILS
    // ─────────────────────────────────────────────────────────────

    public static string BuildAdminDoctorRegistrationEmail(
        string adminName, string doctorName, string specialty,
        string email, string licenseNumber, DateTime appliedAt)
    {
        var content = $@"
            <p>Hello <strong>{adminName}</strong>,</p>
            <p>A new doctor has submitted a registration application on MediSphere and requires your review.</p>
            <div class='info-box'>
                <table class='info-table'>
                    <tr><td class='info-label'>Doctor Name</td><td class='info-colon'>:</td><td class='info-value'>Dr. {doctorName}</td></tr>
                    <tr><td class='info-label'>Specialty</td><td class='info-colon'>:</td><td class='info-value'>{specialty}</td></tr>
                    <tr><td class='info-label'>Email</td><td class='info-colon'>:</td><td class='info-value'>{email}</td></tr>
                    <tr><td class='info-label'>License No.</td><td class='info-colon'>:</td><td class='info-value'>{licenseNumber}</td></tr>
                    <tr><td class='info-label'>Applied At</td><td class='info-colon'>:</td><td class='info-value'>{appliedAt:dd-MMM-yyyy HH:mm} UTC</td></tr>
                    <tr><td class='info-label'>Status</td><td class='info-colon'>:</td><td class='info-value'><span class='badge badge-warning'>Pending Review</span></td></tr>
                </table>
            </div>
            <p>Please log into the Admin Panel to review, approve, or reject this application.</p>";

        return BuildHtmlTemplate("New Doctor Registration", "Action Required: New Doctor Application", content,
            "https://medisphere.app/admin", "Go to Admin Panel");
    }

    public static string BuildAdminNewAppointmentEmail(
        string adminName, string patientName, string doctorName, string date, string time)
    {
        var content = $@"
            <p>Hello <strong>{adminName}</strong>,</p>
            <p>A new appointment has been scheduled on the platform.</p>
            <div class='info-box'>
                <table class='info-table'>
                    <tr><td class='info-label'>Patient</td><td class='info-colon'>:</td><td class='info-value'>{patientName}</td></tr>
                    <tr><td class='info-label'>Doctor</td><td class='info-colon'>:</td><td class='info-value'>Dr. {doctorName}</td></tr>
                    <tr><td class='info-label'>Date &amp; Time</td><td class='info-colon'>:</td><td class='info-value'>{date} at {time}</td></tr>
                </table>
            </div>
            <p>Access the Admin Panel to manage appointments, clinical schedules, and queue states.</p>";

        return BuildHtmlTemplate("New Appointment Scheduled", "Platform Activity Alert", content,
            "https://medisphere.app/admin", "Go to Admin Panel");
    }

    public static string BuildAdminReferralActivityEmail(
        string adminName, string referrerName, string refereeName, int pointsReferrer, int pointsReferee)
    {
        var content = $@"
            <p>Hello <strong>{adminName}</strong>,</p>
            <p>New referral activity has been completed on the platform.</p>
            <div class='info-box'>
                <table class='info-table'>
                    <tr><td class='info-label'>Referrer (Earned {pointsReferrer} pts)</td><td class='info-colon'>:</td><td class='info-value'>{referrerName}</td></tr>
                    <tr><td class='info-label'>Referred Patient (Earned {pointsReferee} pts)</td><td class='info-colon'>:</td><td class='info-value'>{refereeName}</td></tr>
                    <tr><td class='info-label'>Activity Type</td><td class='info-colon'>:</td><td class='info-value'>First Booking Reward</td></tr>
                </table>
            </div>
            <p>Access the referral management suite to monitor points ledger and transactions.</p>";

        return BuildHtmlTemplate("Referral Rewards Credited", "Platform Referral Activity", content,
            "https://medisphere.app/admin", "Go to Admin Panel");
    }
    public static string BuildAppointmentRescheduledEmail(
    string patientName, string doctorName, string specialty,
    string newDate, string newTime, int queueToken)
    {
        var content = $@"
        <p>Hello <strong>{patientName}</strong>,</p>
        <p>Your appointment has been <strong>rescheduled</strong>. Here are your updated details:</p>
        <div class='info-box'>
            <table class='info-table'>
                <tr><td class='info-label'>Doctor</td><td class='info-colon'>:</td><td class='info-value'>Dr. {doctorName} ({specialty})</td></tr>
                <tr><td class='info-label'>New Date</td><td class='info-colon'>:</td><td class='info-value'>{newDate}</td></tr>
                <tr><td class='info-label'>New Time</td><td class='info-colon'>:</td><td class='info-value'>{newTime}</td></tr>
                <tr><td class='info-label'>Queue Token</td><td class='info-colon'>:</td><td class='info-value'>#{queueToken}</td></tr>
                <tr><td class='info-label'>Status</td><td class='info-colon'>:</td><td class='info-value'><span class='badge badge-info'>Rescheduled</span></td></tr>
            </table>
        </div>
        <p>Please make a note of your new appointment time. If this doesn't work for you, you can reschedule again from your dashboard.</p>
        <p>Thank you for choosing MediSphere! &#128153;</p>";

        return BuildHtmlTemplate("Appointment Rescheduled", "Your Appointment Has Been Rescheduled", content,
            "https://medisphere.app/appointments/history", "View My Appointments");
    }

    public static string BuildDoctorAppointmentCompletedEmail(
        string doctorName, string patientName, string date, string time, decimal earnings)
    {
        var content = $@"
        <p>Hello <strong>Dr. {doctorName}</strong>,</p>
        <p>A consultation has been marked as <strong>completed</strong>.</p>
        <div class='info-box'>
            <table class='info-table'>
                <tr><td class='info-label'>Patient</td><td class='info-colon'>:</td><td class='info-value'>{patientName}</td></tr>
                <tr><td class='info-label'>Date</td><td class='info-colon'>:</td><td class='info-value'>{date}</td></tr>
                <tr><td class='info-label'>Time</td><td class='info-colon'>:</td><td class='info-value'>{time}</td></tr>
                <tr><td class='info-label'>Earnings Credited</td><td class='info-colon'>:</td><td class='info-value'>&#8377;{earnings:N2}</td></tr>
                <tr><td class='info-label'>Status</td><td class='info-colon'>:</td><td class='info-value'><span class='badge badge-success'>Completed</span></td></tr>
            </table>
        </div>
        <p>Your earnings have been updated in your dashboard. Thank you for delivering quality healthcare! &#128153;</p>";

        return BuildHtmlTemplate("Consultation Completed", "Consultation Marked as Completed", content,
            "https://medisphere.app/doctor-dashboard", "View Earnings Panel");
    }

    public static string BuildDoctorAppointmentCancelledEmail(
        string doctorName, string patientName, string date, string time)
    {
        var content = $@"
        <p>Hello <strong>Dr. {doctorName}</strong>,</p>
        <p>An appointment in your schedule has been <strong>cancelled</strong>.</p>
        <div class='info-box'>
            <table class='info-table'>
                <tr><td class='info-label'>Patient</td><td class='info-colon'>:</td><td class='info-value'>{patientName}</td></tr>
                <tr><td class='info-label'>Date</td><td class='info-colon'>:</td><td class='info-value'>{date}</td></tr>
                <tr><td class='info-label'>Time</td><td class='info-colon'>:</td><td class='info-value'>{time}</td></tr>
                <tr><td class='info-label'>Status</td><td class='info-colon'>:</td><td class='info-value'><span class='badge badge-danger'>Cancelled</span></td></tr>
            </table>
        </div>
        <p>This slot is now free in your schedule. Your dashboard has been updated accordingly.</p>";

        return BuildHtmlTemplate("Appointment Cancelled", "An Appointment Was Cancelled", content,
            "https://medisphere.app/doctor-dashboard", "View Dashboard");
    }

    public static string BuildDoctorAppointmentRescheduledEmail(
        string doctorName, string patientName, string newDate, string newTime)
    {
        var content = $@"
        <p>Hello <strong>Dr. {doctorName}</strong>,</p>
        <p>An appointment in your schedule has been <strong>rescheduled</strong>.</p>
        <div class='info-box'>
            <table class='info-table'>
                <tr><td class='info-label'>Patient</td><td class='info-colon'>:</td><td class='info-value'>{patientName}</td></tr>
                <tr><td class='info-label'>New Date</td><td class='info-colon'>:</td><td class='info-value'>{newDate}</td></tr>
                <tr><td class='info-label'>New Time</td><td class='info-colon'>:</td><td class='info-value'>{newTime}</td></tr>
                <tr><td class='info-label'>Status</td><td class='info-colon'>:</td><td class='info-value'><span class='badge badge-info'>Rescheduled</span></td></tr>
            </table>
        </div>
        <p>Please review your updated schedule in the doctor dashboard.</p>";

        return BuildHtmlTemplate("Appointment Rescheduled", "An Appointment Has Been Rescheduled", content,
            "https://medisphere.app/doctor-dashboard", "View Dashboard");
    }
}