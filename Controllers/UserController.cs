using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using task4.Models;

namespace task4.Controllers
{
    public class UserController : Controller
    {
        private readonly UserManager<User> userManager;

        private readonly SignInManager<User> signInManager;

        public UserController(UserManager<User> userManager, SignInManager<User> signInManager)
        {
            this.userManager = userManager;
            this.signInManager = signInManager;
        }

        [HttpPost]
        public async Task<IActionResult> SignUp(RegisterViewModel model)
        {
            if (!CheckNewUserData(model)) return RedirectToAction("SignUp", "Account");
            var user = CreateNewUser(model);
            var result = await userManager.CreateAsync(user, model.Password);
            return await GetSignUpResult(result, user);
        }

        private bool CheckNewUserData(RegisterViewModel model)
        {
            return model != null && !string.IsNullOrEmpty(model.Email)
                && !string.IsNullOrEmpty(model.Name) && !string.IsNullOrEmpty(model.Password);
        }

        private User CreateNewUser(RegisterViewModel model)
        {
            return new User
            {
                UserName = model.Email,
                Name = model.Name,
                Email = model.Email,
                RegistrationDate = DateTime.Now,
                LastLoginDate = DateTime.Now,
                IsBlocked = false
            };
        }

        private async Task<IActionResult> GetSignUpResult(IdentityResult result, User user)
        {
            if (result.Succeeded)
            {
                await signInManager.SignInAsync(user, isPersistent: false);
                return RedirectToAction("UsersInfo");
            }
            else return RedirectToAction("SignUp", "Account");
        }

        [Authorize]
        public ActionResult UsersInfo()
        {
            var users = userManager.Users.ToList();
            return View(users);
        }

        [HttpPost]
        public async Task<IActionResult> LogIn(LoginViewModel model)
        {
            if (!CheckUserLoginData(model)) return RedirectToAction("LogIn", "Account");
            var user = await userManager.FindByEmailAsync(model.Email);
            if (user == null) return RedirectToAction("LogIn", "Account");
            else return await LoginUser(user, model);
        }

        private bool CheckUserLoginData(LoginViewModel model)
        {
            return model != null && !string.IsNullOrEmpty(model.Email)
                    && !string.IsNullOrEmpty(model.Password);
        }

        private async Task<IActionResult> LoginUser(User user, LoginViewModel model)
        {
            if (user.IsBlocked) return RedirectToAction("LogIn", "Account");
            var result = await signInManager.PasswordSignInAsync(user, model.Password, isPersistent: false, lockoutOnFailure: false);
            return await HandleLogInResult(result, user);
        }

        private async Task<IActionResult> HandleLogInResult(Microsoft.AspNetCore.Identity.SignInResult result, User user)
        {
            if (result.Succeeded)
            {
                user.LastLoginDate = DateTime.Now;
                await userManager.UpdateAsync(user);
                return RedirectToAction("UsersInfo", "User");
            }
            else return RedirectToAction("LogIn", "Account");
        }

        public async Task<IActionResult> LogOut()
        {
            await signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        public async Task<IActionResult> Block([FromBody] SelectedUsersModel model)
        {
            if (!CheckSelectedUsers(model)) return GetJSONFailureMessage("No users selected.");
            return await UpdateSelectedUsersBlockStatus(model, true);
        }

        [HttpPost]
        public async Task<IActionResult> Unblock([FromBody] SelectedUsersModel model)
        {
            if (!CheckSelectedUsers(model)) return GetJSONFailureMessage("No users selected.");
            return await UpdateSelectedUsersBlockStatus(model, false);
        }

        private async Task<IActionResult> UpdateSelectedUsersBlockStatus(SelectedUsersModel model, bool block)
        {
            bool userBlockedSelf = false;
            foreach (var userId in model.selectedItem)
            {
                if (userId == userManager.GetUserId(User)) userBlockedSelf = true;
                await UpdateUserBlockStatus(userId.ToString(), block);
            }
            return userBlockedSelf ? await SignUserOut() : GetJSONSuccessMessage();
        }

        private async Task UpdateUserBlockStatus(string userId, bool status)
        {
            var user = await userManager.FindByIdAsync(userId);
            if (user == null) return;
            user.IsBlocked = status;
            await userManager.UpdateAsync(user);
        }

        [HttpPost]
        public async Task<IActionResult> Delete([FromBody] SelectedUsersModel model)
        {
            if (!CheckSelectedUsers(model)) return GetJSONFailureMessage("No users selected.");
            return await DeleteSelectedUsers(model);
        }

        private async Task<IActionResult> DeleteSelectedUsers(SelectedUsersModel model)
        {
            bool userDeletedSelf = false;
            foreach (var userId in model.selectedItem)
            {
                if (userId == userManager.GetUserId(User)) userDeletedSelf = true;
                await DeleteUser(userId);
            }
            return userDeletedSelf ? await SignUserOut() : GetJSONSuccessMessage();
        }

        private async Task DeleteUser(string userId)
        {
            var user = await userManager.FindByIdAsync(userId);
            if (user == null) return;
            await userManager.DeleteAsync(user);
        }

        private async Task<IActionResult> SignUserOut()
        {
            await signInManager.SignOutAsync();
            return GetJSONSuccessMessage(Url.Action("Index", "Home"));
        }

        private JsonResult GetJSONFailureMessage(string message)
        {
            return Json(new { success = false, message });
        }

        private JsonResult GetJSONSuccessMessage(string inRedirectUrl = null)
        {
            return Json(new { success = true, redirectUrl = inRedirectUrl});
        }

        public bool CheckSelectedUsers(SelectedUsersModel model)
        {
            if (model == null || model.selectedItem == null || model.selectedItem.Count == 0) return false;
            var selectedItem = model.selectedItem;
            return selectedItem != null && selectedItem.Count != 0;
        }
    }
}

