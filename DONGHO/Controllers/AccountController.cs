using System;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using MEGATECH.Models.EF;
using MEGATECH.MaHoa;

namespace MEGATECH.Controllers
{
    public class AccountController : Controller
    {
        private readonly MEGATECHDBContext db = new MEGATECHDBContext();

        public ActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Register(Account model)
        {
            if (ModelState.IsValid)
            {
                var checkUser = db.Accounts.FirstOrDefault(x => x.Username == model.Username);
                if (checkUser != null)
                {
                    ViewBag.error = "Tên đăng nhập đã tồn tại";
                    return View(model);
                }

                var checkEmail = db.Accounts.FirstOrDefault(x => x.Email == model.Email);
                if (checkEmail != null)
                {
                    ViewBag.error = "Email đã được sử dụng";
                    return View(model);
                }

                model.Password = MaHoaDuLieu.MD5Hash(model.Password);
                model.IsActive = true;
                model.IsAdmin = false;
                model.CreatedDate = DateTime.Now;
                model.ModifiedDate = DateTime.Now;

                db.Accounts.Add(model);
                db.SaveChanges();

                return RedirectToAction("Login");
            }
            return View(model);
        }

        public ActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(string Username, string Password, bool RememberMe = false)
        {
            if (string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(Password))
            {
                ViewBag.error = "Vui lòng nhập đầy đủ thông tin";
                return View();
            }

            var hashedPassword = MaHoaDuLieu.MD5Hash(Password);
            var user = db.Accounts.FirstOrDefault(x => x.Username == Username && x.Password == hashedPassword);

            if (user != null)
            {
                if (!user.IsActive)
                {
                    ViewBag.error = "Tài khoản đã bị khóa";
                    return View();
                }

                var ticket = new FormsAuthenticationTicket(
                    1,
                    user.Username,
                    DateTime.Now,
                    DateTime.Now.AddDays(RememberMe ? 30 : 1),
                    RememberMe,
                    user.IsAdmin.ToString(),
                    FormsAuthentication.FormsCookiePath);

                var encryptedTicket = FormsAuthentication.Encrypt(ticket);
                var cookie = new HttpCookie(FormsAuthentication.FormsCookieName, encryptedTicket);

                if (RememberMe)
                    cookie.Expires = ticket.Expiration;

                Response.Cookies.Add(cookie);

                if (user.IsAdmin)
                    return RedirectToAction("Index", "Home", new { area = "Admin" });
                return RedirectToAction("Index", "Home");
            }

            ViewBag.error = "Tên đăng nhập hoặc mật khẩu không đúng";
            return View();
        }

        public ActionResult Logout()
        {
            FormsAuthentication.SignOut();
            return RedirectToAction("Index", "Home");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}