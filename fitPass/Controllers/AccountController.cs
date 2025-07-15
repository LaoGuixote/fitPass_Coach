using Microsoft.AspNetCore.Mvc;
using fitPass.Models;
using System.Linq;
using fitPass.Models;


public class AccountController : Controller
{
    private readonly GymManagementContext _context;

    public AccountController(GymManagementContext context)
    {
        _context = context;
    }

    [HttpGet]
    public IActionResult Login()
    {
        return View();
    }

    [HttpPost]
    public IActionResult Login(string email, string password)
    {
        var user = _context.Accounts.FirstOrDefault(a => a.Email == email && a.IsActive == true);

        if (user == null || user.PasswordHash != password)
        {
            ViewBag.Error = "帳號或密碼錯誤";
            return View();
        }

        // 記錄登入資訊
        HttpContext.Session.SetInt32("MemberId", user.MemberId);
        HttpContext.Session.SetString("UserName", user.Name);
        HttpContext.Session.SetInt32("Admin", user.Admin);

        user.LastLoginTime = DateTime.Now;
        _context.SaveChanges();

        // 導向角色對應首頁
        return RedirectToAction("Index", user.Admin switch
        {
            3 => "Admin",
            2 => "Coach",
            _ => "Member"
        });
    }

    [HttpGet]
    public IActionResult Register()
    {
        return View();
    }

    [HttpPost]
    public IActionResult Register(string email, string password, string confirmPassword, string name, string phone)
    {
        if (password != confirmPassword)
        {
            ViewBag.Error = "兩次輸入的密碼不一致";
            return View();
        }

        if (_context.Accounts.Any(a => a.Email == email))
        {
            ViewBag.Error = "此信箱已被註冊";
            return View();
        }

        var account = new Account
        {
            Email = email,
            PasswordHash = password, 
            Name = name,
            Phone = phone,
            Admin = 1,
            IsActive = true,
            JoinDate = DateOnly.FromDateTime(DateTime.Now),
            Type = 1,                 
            Point = 0                
        };

        _context.Accounts.Add(account);
        _context.SaveChanges();

        // 自動登入
        HttpContext.Session.SetInt32("MemberId", account.MemberId);
        HttpContext.Session.SetString("UserName", account.Name);
        HttpContext.Session.SetInt32("Admin", account.Admin);

        return RedirectToAction("Index", "Member");
    }





    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return RedirectToAction("Login");
    }
}
