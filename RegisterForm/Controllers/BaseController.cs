namespace RegisterForm.Controllers
{
    public class BaseController
    {
        public Dictionary<string, string> Session { get; set; } = new Dictionary<string, string>();
        public Guid? CurrentUserId { get; set; }
        public string CurrentUserUsername { get; set; } = string.Empty;
        protected virtual string View(string viewName = null, object model = null)
        {
            string controllerFolder = this.GetType().Name.Replace("Controller", "");
            viewName ??= this.GetType().Name.Replace("Controller", "") + ".html";
            string path = Path.Combine("Views", controllerFolder, viewName);
            string html = File.Exists(path) ? File.ReadAllText(path) : "<h1>View not found</h1>";

            if (model != null)
            {
                var dict = new Dictionary<string, string>();

                foreach (var prop in model.GetType().GetProperties())
                {
                    var val = prop.GetValue(model);

                    if (val is IDictionary<string, string> errorsDict)
                    {
                        foreach (var kv in errorsDict)
                            dict[kv.Key + "Error"] = kv.Value;
                    }
                    else if (val != null && !val.GetType().IsPrimitive && !(val is string))
                    {
                        foreach (var nestedProp in val.GetType().GetProperties())
                        {
                            var nestedValue = nestedProp.GetValue(val)?.ToString() ?? "";
                            dict[nestedProp.Name] = nestedValue;
                        }
                    }
                    else
                    {
                        dict[prop.Name] = val?.ToString() ?? "";
                    }
                }

                foreach (var kv in dict)
                {
                    string placeholder = "{{" + kv.Key + "}}";
                    html = html.Replace(placeholder, kv.Value);
                }
            }

            html = System.Text.RegularExpressions.Regex.Replace(html, @"\{\{.*?\}\}", "");

            return html;
        }


        protected string RedirectToAction(string action, string controller = null)
        {
            string target = controller != null ? $"/{controller}/{action}" : $"/{action}";
            return $"REDIRECT:{target}";
        }
        protected string Json(object obj)
        {
            return System.Text.Json.JsonSerializer.Serialize(obj);
        }
    }
}
