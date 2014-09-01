using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using Kartverket.MetadataMonitor.Models.Auth;
using System.Configuration;

namespace Kartverket.MetadataMonitor.Controllers
{
    public class AccountController : Controller
    {

        public ActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Login(User model, string returnUrl)
        {
            if (ModelState.IsValid)
            {
                string username = model.Username;
                string password = model.Password;

                string hardcodedUsername = ConfigurationManager.AppSettings["LoginAdminUsername"];
                string hardcodedPassword = ConfigurationManager.AppSettings["LoginAdminPassword"];

                bool userValid = username.Equals(hardcodedUsername) && password.Equals(hardcodedPassword);
                
                //WeLoveInspire13

                if (userValid)
                {
                    FormsAuthentication.SetAuthCookie(username, true);
                    if (Url.IsLocalUrl(returnUrl) && returnUrl.Length > 1 && returnUrl.StartsWith("/")
                        && !returnUrl.StartsWith("//") && !returnUrl.StartsWith("/\\"))
                    {
                        return Redirect(returnUrl);
                    }
                    else
                    {
                        return RedirectToAction("Index", "Validator");
                    }
                }
                else
                {
                    ModelState.AddModelError("", "The user name or password provided is incorrect.");
                }
            }

            return View(model);
        }

        public ActionResult Logout()
        {
            FormsAuthentication.SignOut();

            return RedirectToAction("Index", "Validator");
        }

    }
}
