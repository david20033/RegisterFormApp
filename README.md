# RegisterFormApp
Web Application for register and login

## 1. Използвани технологии
- **ASP.NET Core MVC** - за изграждане на сървърната част и Model-View-Controller архитектурата.
- **Entity Framework Core** – за работа с базата данни.
- **SQL Server** - за съхранение на данните.
- **Razor Pages и HTML/CSS/JavaScript** - за front-end частта на приложението.
- **XUnit и Moq** - За Unit тестове на функционалностите

## 2. Функционалности и тяхната реализация

### Функционалност: Валидация на данните (имейл, имена, парола и др.)

#### 1. Модел на данните
Създадох следните **ViewModel-и**, които съдържат нужните полета за работа с данните:  
- `RegisterViewModel`  
- `LoginViewModel`  
- `EditProfileViewModel`  
- `ChangePasswordViewModel`  

Полета: име, фамилия, имейл, парола, потвърди парола и др.  

За валидация на данните във ViewModel-ите използвах **DataAnnotations** (готови атрибути от ASP.NET Core).  
Добавих правила като:  
- задължителни полета  
- минимална дължина  
- правилен формат на имейл и телефонен номер  
- съвпадение на пароли  
- възрастови граници  

##### Пример от кода:
```csharp
[Required(ErrorMessage = "First name is required")]
[StringLength(50, MinimumLength = 2, ErrorMessage = "First name must be between 2 and 50 characters")]
[RegularExpression(@"^[a-zA-Z]+$", ErrorMessage = "First name can only contain letters.")]
public string FirstName { get; set; } = string.Empty;

[Required(ErrorMessage = "Last name is required")]
[StringLength(50, MinimumLength = 2, ErrorMessage = "Last name must be between 2 and 50 characters")]
[RegularExpression(@"^[a-zA-Z]+$", ErrorMessage = "Last name can only contain letters.")]
public string LastName { get; set; } = string.Empty;

[Required(ErrorMessage = "Email is required")]
[EmailAddress(ErrorMessage = "Invalid email format")]
[StringLength(100)]
public string Email { get; set; } = string.Empty;
```
#### 2. Сърварна валидация
- Когато потребителят изпрати формата (POST заявка), всички правила се проверяват.
- Ако има грешки, те се връщат към View-то и се показват на потребителя.
- Сървърната валидация гарантира, че дори ако потребителят заобиколи клиентската валидация (например чрез Postman), данните пак ще бъдат проверени.
#### 3. Клиентска  валидаци
- Чрез външни скриптове като jquery-validation и DataAnnotations автоматично се генерира и клиентска валидация, без да се чака отговор от сървъра

### Функционалност: Записване на данните в релационна база данни

#### 1. Entity модел
- Създадох клас **`Users`**, който използвам като **Entity модел**.  
- В него са описани колоните:  
  - `Id`  
  - `FirstName`  
  - `LastName`  
  - `Email`  
  - `Password`  
  - `Username`  
  - `DateOfBirth`  
  - `PhoneNumber`  
  - `IsEmailConfirmed`  
- Добавих правила чрез **DataAnnotations** за:  
  - първичен ключ  
  - задължителни полета  
  - минимална/максимална дължина на `string`-овете  

---

#### 2. DBContext и конфигурация
- Създадох клас **`RegisterFormDbContext`**, който наследява `DbContext`.
```csharp
public class RegisterFormDbContext : DbContext
```
- Конфигурирах конструктора
```csharp
   public RegisterFormDbContext(DbContextOptions<RegisterFormDbContext> options) : base(options)
   {
   }
```
- Създадох property Users в класа, което EF Core ще ползва за  връзка с таблица Users
```csharp
  public virtual DbSet<Users> Users { get; set; }
````
- Използвайки Fluent Api зададох при създаването на таблицата колони Email, Phonenumber и Username да бъдат уникални
```csharp
protected override void OnModelCreating(ModelBuilder builder)
{
    base.OnModelCreating(builder);
    builder.Entity<Users>()
        .HasIndex(u => u.Email)
        .IsUnique();
    builder.Entity<Users>()
        .HasIndex(u => u.PhoneNumber)
        .IsUnique();
    builder.Entity<Users>()
        .HasIndex(u => u.Username)
        .IsUnique();
}
```
- В appsettings.json записах connectionString-а към SqlServer
```csharp
    "ConnectionStrings": {
        "DefaultConnectionString": "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=master;Integrated Security=True;Connect Timeout=30;Encrypt=False;Trust Server Certificate=False;Application Intent=ReadWrite;Multi Subnet Failover=False;Database=RegisterForm"
    },
```
- В program.cs регистрирах контекста
```csharp
var connectionString = builder.Configuration.GetConnectionString("DefaultConnectionString");
builder.Services.AddDbContext<RegisterFormDbContext>(options => options.UseSqlServer(connectionString));
```

#### 3. Migrations
- Създадох миграции чрез **Entity Framework Core**:  
  ```bash
  dotnet ef migrations add InitialCreate
  ```
  
- Създадох база данни и таблица чрез миграциите и EF Core
  ```bash
  dotnet ef database update
  ```
  #### 4. Запис и обработка на данни
- Имам няколко метода, които чрез **Entity Framework Core** извличат и/или обработват данните от базата.  
- Пример:  
  - **`CreateUserAsync`** – създава нов потребител.  

Процесът за създаване на потребител е следният:  
1. Взимам данните от **`RegisterViewModel`**.  
2. Мапвам всяко свойство от модела към **`User Entity`**.  
3. Чрез **EF Core** добавям потребителя в базата данни.  
4. Запазвам промените в базата чрез метода `SaveChangesAsync()`.
```csharp
public async Task<Users> CreateUserAsync(RegisterViewModel model)
{
    var user = Users.MapFromRegisterViewModel(model);
    user.Password = new PasswordHasher<Users>().HashPassword(user, model.Password);
    await _context.Users.AddAsync(user);
    await _context.SaveChangesAsync();
    return user;
}
```

### Функционалност: Login  

#### 1. Приемане и валидация на данните
- В контролера се използва **`LoginViewModel`**, който съдържа полета за:  
  - имейл  
  - парола  
  - remember me опция  

---

#### 2. Кепча защита
- Кепча кодът се съхранява в **Session**.  
- Когато потребителят отвори **Login страницата**:  
  - извлича се кепча кодът от сесията  
  - генерира се снимка с кода и линии за по-трудното му четене  
- При грешен кепча код:  
  - генерира се нов код  
  - показва се съобщение за грешка  

---

#### 3. Валидация на потребителя
- Използва се метод **`ValidateLoginAsync`**, който проверява:  
  - дали имейлът и въведената парола съвпадат  
  - дали е премината Кепча проверката  
- При неуспешна валидация се показва подходящо съобщение за грешка.  

---

#### 4. Създаване на сесия
- При успешен вход се извиква метод, който създава **Cookie** чрез **ASP.NET Core Identity / Cookie Authentication**.  
- В Cookie-то се запазва информация дали потребителят е логнат.  
- Ако **Remember me** е маркирано → Cookie-то е с удължена валидност.  

---

### Функционалност: Logout  

#### 1. Метод в контролера
- При извикване на **POST метода за logout** в контролера:  
  - Cookie-то се премахва  
  - текущата сесия на потребителя се прекратява

### Функционалност: Промяна на имена и парола на потребителя  

#### 1. ViewModel-и за редакция
- Използват се два ViewModel-а:  
  - **`EditProfileViewModel`** – съдържа информация за потребителя (без паролата).  
  - **`ChangePasswordViewModel`** – съдържа полета за нова парола, стара парола, потвърждение на паролата, CAPTCHA и Id (за предаване между Controller и View).  
- И в двата ViewModel-а са зададени правила за валидация на данните.  

---

#### 2. Методите в контролера
- Методът **`Edit`** приема модела от формата и проверява дали въведените данни са валидни чрез `ModelState.IsValid`.  
- Идентификаторът на текущо логнатия потребител се взема от системата → гарантира се, че всеки може да редактира само собствения си профил.  
- След това се извиква метод, който обновява данните в базата данни чрез **EF Core**.  

---

#### 3. Потвърждение и обратна връзка
- При успешна промяна се записва съобщение в **`TempData["SuccessMessage"]`**, което може да бъде показано на следващата страница.  
- Потребителят се пренасочва към **`Home/Index`**.  
- Ако данните не са валидни → остава на същата страница и се показват грешките от `ModelState`.  

---

### Функционалност: Генериране на CAPTCHA код и изображение  

#### 1. Генериране на CAPTCHA код
- Създава се произволен **5-символен низ** от букви (A–Z) и цифри (0–9).  
- Използва се **Random** за избор на символите.  

---

#### 2. Генериране на изображение с CAPTCHA
- Създава се **растерно изображение (bitmap)**.  
- Използва се **System.Drawing** за рисуване върху изображение.  
- Задава се цвят на фона.  
- Рисува се CAPTCHA кодът.  
- Добавят се няколко линии с произволни координати, за по-трудно разпознаване на текста.  

---

#### 3. Използване в системата
- CAPTCHA кодът се съхранява в **Session**.  
- Изображението се изпраща към клиента като **масив от байтове**.  
- В HTML се визуализира чрез елемент **`<img>`**.  

---

### Функционалност: Пълно покритие с Unit тестове  

#### 1. Структура на тестовете
- Използвах **xUnit** като framework за unit тестове.  
- Всеки метод от бизнес логиката е тестван с различни сценарии.  
- За методите, които взаимодействат с базата, е използван **in-memory DbContext** на EF Core → избягва се зависимост от реална база.  
- Използвах и **Moq** за методи с външни зависимости.  

---

#### 2. Покритие на всички сценарии
- Всички методи в **`AccountRepository`**.  
- Всички методи в **`AccountService`**.  
- Всички методи в **`AuthService`**.  
- Всички методи в **`AccountController`**.
![Test Explorer image](Screenshots/test.png)

## 3. Използвани готови функции  

### MVC Controller – Функции, Класове, Интерфейси и методи използвани за контрол и рендиране на View  

- **Controller** – базовия клас на Controller. Осигурява достъп до HttpContext, ModelState, View(), RedirectToAction() и др.  
- **IActionResult** – Интерфейс, готов за обработка на отговори като View, Json, RedirectToAction и други.  

---

### Атрибути – Контрол на HTTP, защита от Cross-Site Request Forgery, ограничаване на достъпа (Authorization)  
- **[HttpPost]** – Указва, че действието е POST.  
- **[ValidateAntiForgeryToken]** – Защита от CSRF.  
- **[Authorize]** – Ограничаване на достъпа само за логнати потребители.  

---

### User / Claims – Информация за текущо логнатия потребител  
- **User.Identity.IsAuthenticated** – Проверява дали потребителят е логнат.  
- **User.FindFirst("UserId")?.Value** – Взема claim-а "UserId", който е зададен при SignInAsync, и го връща.  

---

### HttpContext / Session – Съхраняване на временни данни в сесия  
- **SetString(key, value)** – Записване на данни в сесията.  
- **GetString(key)** – Извличане на данни от сесията.  

---

### ModelState – Валидация на входни модели  
- **ModelState.IsValid** – Свойство, което проверява дали моделът е валиден според DataAnnotations.  
- **ModelState.AddModelError(key, message)** – Функция за добавяне на грешка към модела във View.  

---

### HttpContext / SignInAsync / SignOutAsync – Контрол на Cookies  
- **SignInAsync** – Създава Cookie на потребителя, за да се следи дали е логнат в приложението.  
- **SignOutAsync** – Изчиства Cookie на потребителя.  

---

### Entity Framework Core – Достъп и обработка в базата данни  
- **FirstOrDefaultAsync()** – Връща първия запис, който отговаря на дадено условие или връща Null ако няма такъв.  
- **AddAsync(User)** – Добавяне на нов user в БД.  
- **SaveChangesAsync()** – Записва промените в БД.  
- **LINQ функции** като: `Select()`, `Any()`.  

---

### PasswordHasher – Хеширане (криптиране) на парола  
- **HashPassword(user, "SecurePassword123!")** – за хеширане на паролата на потребителя преди да бъде записана/проверена в базата данни.  

---

### Random – Генериране на рандъм числа  
- **Random.Next()** – Генериране на рандъм координати (на линиите, в Captcha изображението) и индекси (за генериране на string от рандъм символи за Captcha кода).  

---

### System.Drawing – Създаване на изображения  
- **Bitmap** – Създаване на растерно изображение.  
- **Graphics.FromImage()** – Рисуване върху изображение.  
- **g.Clear(Color.LightGray)** – Оцветява фона.  
- **g.DrawString(...)** – Рисува текст (CAPTCHA кода).  
- **g.DrawLine(...)** – Рисува линии за по-трудна разпознаваемост на CAPTCHA.  
- **Brushes.Black, Pen(Color, thickness)** – Класове за четки и писалки.  
- **bitmap.Save(ms, ImageFormat.Png)** – Запазване на изображението в stream.

# Файлове с код и за какво се отнасят

## 1. Data
- **RegisterFormDbContext** – Контекстът на базата данни. Отговаря за връзката между приложението и базата данни чрез **EF Core**.  
- **Users** – Entity за потребителите, което включва полета като: Име, Фамилия, Имейл и др. Зададени са правила за валидация на данните преди да бъдат записани в базата данни чрез **DataAnnotations**.  

---

## 2. Repository
- **AccountRepository** – Отговаря за връзката между базата данни и приложението, като извлича/записва данни от базата.  
  Включва методите:  
  - `CreateUserAsync`  
  - `GetUserByEmailAsync`  
  - `GetUserByIdAsync`  
  - `IsEmailTakenAsync`  
  - `IsPhoneNumberTakenAsync`  
  - `IsUsernameTakenAsync`  
  - `UpdateUserProfile`  
  - `UpdateUserPassword`  
- **IAccountRepository** – Интерфейс, който дефинира методите на repository-то.  

---

## 3. Service
- **AccountService** – Отговаря за бизнес логиката на приложението, свързвайки Controller-а с repository-то.  
  Включва методите:  
  - `CreateUserAsync`  
  - `ValidateRegistrationAsync`  
  - `ValidateLoginAsync`  
  - `ChangePasswordAsync`  
  - `GetUserByIdAsync`  
  - `VerifyPasswordAsync`  
  - `MapToEditProfileViewModel`  
  - `UpdateUserProfile`  
  - `GenerateCaptchaCode`  
  - `GenerateCaptchaImage`  
- **IAccountService** – Интерфейс, който дефинира методите на AccountService.  
- **AuthService** – Отговаря за логиката за създаване/премахване на Auth cookies.  
  Включва методите: `SignInAsync`, `SignOutAsync`, `CreatePrincipal`.  
- **IAuthService** – Интерфейс, който дефинира методите на AuthService.  

---

## 4. Controller
- **AccountController** – Управлява и обработва **GET** и **POST** заявки и връща **View** като отговор.  
  Включва методите:  
  - `Register`  
  - `Login`  
  - `Logout`  
  - `Edit`  
  - `ChangePassword`  
  - `CaptchaImage`  

---

## 5. Views
- **Account/Register** и **Account/Login** – HTML формите за регистрация и логин, използващи Razor синтаксис и Tag Helpers за връзка с моделите.  
- **Account/Edit** и **Account/ChangePassword** – HTML формите за промяна на имена и парола.  

---

## 6. ViewModels
- **ChangePasswordViewModel**, **EditProfileViewModel**, **LoginViewModel**, **RegisterViewModel** – Съдържат само полетата, които ще се обработват/показват във View, и валидират данните чрез DataAnnotations.  

---

## 7. Tests
- **AccountController**, **AccountRepository**, **AccountService**, **AuthService** – Unit тестовете за всеки метод от класовете.  
  Обхващат всички възможни тест сценарии за всеки един метод.

  ---
  
## 8. Attributes
- **AgeRangeAttribute** - Custom атрибут за валидация на възраст спрямо рождена дата




  
