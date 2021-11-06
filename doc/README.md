Jak zainstalować rozszerzenie?
W katalogach, gdzie lądują skompilowane pliki (foldery Release albo Debug) są pliki o rozszerzeniu vsix. Są to instalatory rozszerzenia, która należy uruchomić.

Jak testować rozszerzenie?
1. Testując rozszerzenie uruchamiana jest eksperymentalna instancja Visuala, którą od czasu do czsau należy wyczyścić (ponieważ są tam dodawane nasze wtyczki/rozszerzenia i za każdym razem):
Menu start => Visual Studio 2019 => Reset the Visual Studio 2019 Experimntal Instance
Ewentualnie można sobie wygooglować jak to się robi (jeśli powyższe nie będzie działać).
2. W kataogu solucji mamy dodatkowy katalog TestSolution, na której powinniśmy testować to rozszerzenie (ma on taką samą strukturę jak nasz projekt HUF).

W solucji mamy projekt Test, gdzie testujemy sobie samo okno do tłumaczeń (żeby nie odpalać eksperymentalnej instancji VS). Jest tam skrypt pre-buildowy, który przepisuje kod okna z projektu
rozszerzenia do projektu testów, tak aby mieć aktualne okno.

## Key points to resources project
In one directory there must be *.resx file, which differ only by extension with language, i.e. `sample.resx` and `sample.en.resx`. Main name must be consistent.
Also there has to be designer file with the same name, but with post fix `Designer.cs`, so in example: `sample.Designer.cs`. Directory must be common to all those files (they should be in one directory), but it does not matter if it's directly in project or in Resources directory (just namespace would change, which still can be read from designer file).

## Solution to problem of multilanguage application

Extension was developed based on StackOverflow post on "what is the best way to develop multilanguage application": [link to the SO post](https://stackoverflow.com/questions/373388/best-way-to-implement-multi-language-globalization-in-large-net-project).

<h1>Use a separate project with Resources</h1>

I can tell this from out experience, having a current solution with <strike>12</strike> **24** projects that includes API, MVC, Project Libraries (Core functionalities), WPF, UWP and Xamarin. It is worth reading this long post as I think it is the best way to do so. With the help of VS tools easily exportable and importable to sent to translation agencies or review by other people.

**EDIT 02/2018:** Still going strong, converting it to a .NET Standard library makes it possible to even use it across .NET Framework and NET Core. I added an extra section for converting it to JSON so for example angular can use it.

**EDIT 2019:** Going forward with Xamarin, this still works across all platforms. E.g. Xamarin.Forms advices to use resx files as well. (I did not develop an app in Xamarin.Forms yet, but the documentation, that is way to detailed to just get started, covers it: [Xamarin.Forms Documentation][1]). Just like converting it to JSON we can also convert it to a .xml file for Xamarin.Android.

**EDIT 2019 (2):** While upgrading to UWP from WPF, I encountered that in UWP they prefer to use another filetype `.resw`, which is is in terms of content identical but the usage is different. I found a different way of doing this which, in my opinion, works better then [the default solution][2].

**EDIT 2020:** Updated some suggestions for larger (modulair) projects that might require multiple language projects.

So, lets get to it.

**Pro's**

- Strongly typed almost everywhere.
- In WPF you don't have to deal with `ResourceDirectories`.
- Supported for ASP.NET, Class Libraries, WPF, Xamarin, .NET Core, .NET Standard as far as I have tested.
- No extra third-party libraries needed.
- Supports culture fallback: en-US -> en.
- Not only back-end, works also in XAML for WPF and Xamarin.Forms, in .cshtml for MVC.
- Easily manipulate the language by changing the `Thread.CurrentThread.CurrentCulture`
- Search engines can Crawl in different languages and user can send or save language-specific urls.

**Con's**

 - WPF XAML is sometimes buggy, newly added strings don't show up directly. Rebuild is the temp fix (vs2015).
 - UWP XAML does not show intellisense suggestions and does not show the text while designing.
 - Tell me.

**Setup**

Create language project in your solution, give it a name like *MyProject.Language*. Add a folder to it called Resources, and in that folder, create two Resources files (.resx). One called **Resources.resx** and another called **Resources.en.resx** (or .en-GB.resx for specific). In my implementation, I have NL (Dutch) language as the default language, so that goes in my first file, and English goes in my second file.

Setup should look like this:

[![language setup project][3]][3]

The properties for Resources.resx must be:

[![properties][4]][4]

Make sure that the custom tool namespace is set to your project namespace. Reason for this is that in WPF, you cannot reference to `Resources` inside XAML.

And inside the resource file, set the access modifier to Public:

[![access modifier][5]][5]

If you have such a large application (let's say different modules) you can consider creating multiple projects like above. In that case you could prefix your Keys and resource classes with the particular Module. Use the [best language editor][6] there is for Visual Studio to combine all files into a single overview.

**Using in another project**

Reference to your project: Right click on References -> Add Reference -> Prjects\Solutions.

Use namespace in a file: `using MyProject.Language;`

Use it like so in back-end:
`string someText = Resources.orderGeneralError;`
    If there is something else called Resources, then just put in the entire namespace.

<h3>Using in MVC</h3>

In MVC you can do however you like to set the language, but I used parameterized url's, which can be setup like so:


**RouteConfig.cs**
Below the other mappings

    routes.MapRoute(
        name: "Locolized",
        url: "{lang}/{controller}/{action}/{id}",
        constraints: new { lang = @"(\w{2})|(\w{2}-\w{2})" },   // en or en-US
        defaults: new { controller = "shop", action = "index", id = UrlParameter.Optional }
    );

**FilterConfig.cs** (might need to be added, if so, add `FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);` to the `Application_start()` method in `Global.asax`

    public class FilterConfig
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new ErrorHandler.AiHandleErrorAttribute());
            //filters.Add(new HandleErrorAttribute());
            filters.Add(new LocalizationAttribute("nl-NL"), 0);
        }
    }

**LocalizationAttribute**

    public class LocalizationAttribute : ActionFilterAttribute
    {
        private string _DefaultLanguage = "nl-NL";
        private string[] allowedLanguages = { "nl", "en" };

        public LocalizationAttribute(string defaultLanguage)
        {
            _DefaultLanguage = defaultLanguage;
        }

        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            string lang = (string) filterContext.RouteData.Values["lang"] ?? _DefaultLanguage;
            LanguageHelper.SetLanguage(lang);
        }
    }

**LanguageHelper** just sets the Culture info.

    //fixed number and date format for now, this can be improved.
    public static void SetLanguage(LanguageEnum language)
    {
        string lang = "";
        switch (language)
        {
            case LanguageEnum.NL:
                lang = "nl-NL";
                break;
            case LanguageEnum.EN:
                lang = "en-GB";
                break;
            case LanguageEnum.DE:
                lang = "de-DE";
                break;
        }
        try
        {
            NumberFormatInfo numberInfo = CultureInfo.CreateSpecificCulture("nl-NL").NumberFormat;
            CultureInfo info = new CultureInfo(lang);
            info.NumberFormat = numberInfo;
            //later, we will if-else the language here
            info.DateTimeFormat.DateSeparator = "/";
            info.DateTimeFormat.ShortDatePattern = "dd/MM/yyyy";
            Thread.CurrentThread.CurrentUICulture = info;
            Thread.CurrentThread.CurrentCulture = info;
        }
        catch (Exception)
        {

        }
    }


**Usage in .cshtml**

    @using MyProject.Language;
    <h3>@Resources.w_home_header</h3>

or if you don't want to define usings then just fill in the entire namespace OR you can define the namespace under /Views/web.config:

<!-- language: lang-xml -->

	<system.web.webPages.razor>
	<host factoryType="System.Web.Mvc.MvcWebRazorHostFactory, System.Web.Mvc, Version=5.2.3.0, Culture=neutral, PublicKeyToken=31BF3856AD364E35" />
	<pages pageBaseType="System.Web.Mvc.WebViewPage">
	  <namespaces>
		...
		<add namespace="MyProject.Language" />
	  </namespaces>
	</pages>
	</system.web.webPages.razor>

This mvc implementation source tutorial: [Awesome tutorial blog][7]

**Using in class libraries for models**

Back-end using is the same, but just an example for using in attributes

	using MyProject.Language;
	namespace MyProject.Core.Models
	{
		public class RegisterViewModel
		{
			[Required(ErrorMessageResourceName = "accountEmailRequired", ErrorMessageResourceType = typeof(Resources))]
			[EmailAddress]
			[Display(Name = "Email")]
			public string Email { get; set; }
		}
	}

If you have reshaper it will automatically check if the given resource name exists. If you prefer type safety you can use [T4 templates to generate an enum][8] 


<h3>Using in WPF.</h3>

Ofcourse add a reference to your *MyProject.Language* namespace, we know how to use it in back-end.

In XAML, inside the header of a Window or UserControl, add a namespace reference called `lang` like so:

<!-- language: lang-xml -->

    <UserControl x:Class="Babywatcher.App.Windows.Views.LoginView"
    		 xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    			 xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    			 xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006" 
    			 xmlns:d="http://schemas.microsoft.com/expression/blend/2008" 
    			 xmlns:local="clr-namespace:MyProject.App.Windows.Views"
    			  xmlns:lang="clr-namespace:MyProject.Language;assembly=MyProject.Language" <!--this one-->
    			 mc:Ignorable="d" 
    			d:DesignHeight="210" d:DesignWidth="300">

Then, inside a label:

        <Label x:Name="lblHeader" Content="{x:Static lang:Resources.w_home_header}" TextBlock.FontSize="20" HorizontalAlignment="Center"/>

Since it is strongly typed you are sure the resource string exists. You might need to recompile the project sometimes during setup, WPF is sometimes buggy with new namespaces.

One more thing for WPF, set the language inside the `App.xaml.cs`. You can do your own implementation (choose during installation) or let the system decide.

<!-- language: lang-c# -->

    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            SetLanguageDictionary();
        }

        private void SetLanguageDictionary()
        {
            switch (Thread.CurrentThread.CurrentCulture.ToString())
            {
                case "nl-NL":
                    MyProject.Language.Resources.Culture = new System.Globalization.CultureInfo("nl-NL");
                    break;
                case "en-GB":
                    MyProject.Language.Resources.Culture = new System.Globalization.CultureInfo("en-GB");
                    break;
                default://default english because there can be so many different system language, we rather fallback on english in this case.
                    MyProject.Language.Resources.Culture = new System.Globalization.CultureInfo("en-GB");
                    break;
            }
           
        }
    }

<h3>Using in UWP</h3>

In UWP, Microsoft uses [this solution][2], meaning you will need to create new resource files. Plus you can not re-use the text either because they want you to set the `x:Uid` of your control in XAML to a key in your resources. And in your resources you have to do `Example.Text` to fill a `TextBlock`'s text. I didn't like that solution at all because I want to re-use my resource files. Eventually I came up with the following solution. I just found this out today (2019-09-26) so I might come back with something else if it turns out this doesn't work as desired.

Add this to your project:

    using Windows.UI.Xaml.Resources;

    public class MyXamlResourceLoader : CustomXamlResourceLoader
    {
        protected override object GetResource(string resourceId, string objectType, string propertyName, string propertyType)
        {
            return MyProject.Language.Resources.ResourceManager.GetString(resourceId);
        }
    }

Add this to `App.xaml.cs` in the constructor:

    CustomXamlResourceLoader.Current = new MyXamlResourceLoader();

Where ever you want to in your app, use this to change the language:

    ApplicationLanguages.PrimaryLanguageOverride = "nl";
    Frame.Navigate(this.GetType());

The last line is needed to refresh the UI. While I am still working on this project I noticed that I needed to do this 2 times. I might end up with a language selection at the first time the user is starting. But since this will be distributed via Windows Store, the language is usually equal to the system language.

Then use in XAML:

    <TextBlock Text="{CustomResource ExampleResourceKey}"></TextBlock>

<h3>Using it in Angular (convert to JSON)</h3>

Now days it is more common to have a framework like Angular in combination with components, so without cshtml. Translations are stored in json files, I am not going to cover how that works, I would just highly recommend [ngx-translate][10] instead of the angular multi-translation. So if you want to convert translations to a JSON file, it is pretty easy, I use a T4 template script that converts the Resources file to a json file. I recommend installing [T4 editor][9] to read the syntax and use it correctly because you need to do some modifications.

Only 1 thing to note: It is not possible to generate the data, copy it, clean the data and generate it for another language. So you have to copy below code as many times as languages you have and change the entry before '//choose language here'. Currently no time to fix this but probably will update later (if interested).

*Path: MyProject.Language/T4/CreateLocalizationEN.tt*

    <#@ template debug="false" hostspecific="true" language="C#" #>
    <#@ assembly name="System.Core" #>
    <#@ assembly name="System.Windows.Forms" #>
    <#@ import namespace="System.Linq" #>
    <#@ import namespace="System.Text" #>
    <#@ import namespace="System.Collections.Generic" #>
    <#@ import namespace="System.Resources" #>
    <#@ import namespace="System.Collections" #>
    <#@ import namespace="System.IO" #>
    <#@ import namespace="System.ComponentModel.Design" #>
    <#@ output extension=".json" #>
    <#


	var fileNameNl = "../Resources/Resources.resx";
	var fileNameEn = "../Resources/Resources.en.resx";
	var fileNameDe = "../Resources/Resources.de.resx";
	var fileNameTr = "../Resources/Resources.tr.resx";

	var fileResultName = "../T4/CreateLocalizationEN.json";//choose language here
	var fileResultPath = Path.Combine(Path.GetDirectoryName(this.Host.ResolvePath("")), "MyProject.Language", fileResultName);
	//var fileDestinationPath = "../../MyProject.Web/ClientApp/app/i18n/";

	var fileNameDestNl = "nl.json";
	var fileNameDestEn = "en.json";
	var fileNameDestDe = "de.json";
	var fileNameDestTr = "tr.json";
	
	var pathBaseDestination = Directory.GetParent(Directory.GetParent(this.Host.ResolvePath("")).ToString()).ToString();
	
	string[] fileNamesResx = new string[] {fileNameEn }; //choose language here
	string[] fileNamesDest = new string[] {fileNameDestEn }; //choose language here

	for(int x = 0; x < fileNamesResx.Length; x++)
	{
		var currentFileNameResx = fileNamesResx[x];
		var currentFileNameDest = fileNamesDest[x];
		var currentPathResx = Path.Combine(Path.GetDirectoryName(this.Host.ResolvePath("")), "MyProject.Language", currentFileNameResx);
		var currentPathDest =pathBaseDestination + "/MyProject.Web/ClientApp/app/i18n/" + currentFileNameDest;
		using(var reader = new ResXResourceReader(currentPathResx))
		{
    	    reader.UseResXDataNodes = true;
    #>
			{
    <#
				foreach(DictionaryEntry entry in reader)
				{
					var name = entry.Key;
					var node = (ResXDataNode)entry.Value;
					var value = node.GetValue((ITypeResolutionService) null); 
					 if (!String.IsNullOrEmpty(value.ToString())) value = value.ToString().Replace("\n", "");
					 if (!String.IsNullOrEmpty(value.ToString())) value = value.ToString().Replace("\r", "");
    #>
				"<#=name#>": "<#=value#>",
    <#

		
				}
    #>
			"WEBSHOP_LASTELEMENT": "just ignore this, for testing purpose"
			}
    <#
		}
		File.Copy(fileResultPath, currentPathDest, true);
    }


    #>

If you have a modulair application and you followed my suggestion to create multiple language projects, then you will have to create a T4 file for each of them. Make sure the json files are logically defined, it doesn't have to be `en.json`, it can also be `example-en.json`. To combine multiple json files for using with [ngx-translate][10], follow the instructions [here][11]

<h3>Use in Xamarin.Android</h3>

As explained above in the updates, I use the same method as I have done with Angular/JSON. But Android uses XML files, so I wrote a T4 file that generates those XML files.

*Path: MyProject.Language/T4/CreateAppLocalizationEN.tt*

    #@ template debug="false" hostspecific="true" language="C#" #>
	<#@ assembly name="System.Core" #>
	<#@ assembly name="System.Windows.Forms" #>
	<#@ import namespace="System.Linq" #>
	<#@ import namespace="System.Text" #>
	<#@ import namespace="System.Collections.Generic" #>
	<#@ import namespace="System.Resources" #>
	<#@ import namespace="System.Collections" #>
	<#@ import namespace="System.IO" #>
	<#@ import namespace="System.ComponentModel.Design" #>
	<#@ output extension=".xml" #>
	<#
	var fileName = "../Resources/Resources.en.resx";
	var fileResultName = "../T4/CreateAppLocalizationEN.xml";
	var fileResultRexPath = Path.Combine(Path.GetDirectoryName(this.Host.ResolvePath("")), "MyProject.Language", fileName);
	var fileResultPath = Path.Combine(Path.GetDirectoryName(this.Host.ResolvePath("")), "MyProject.Language", fileResultName);

		var fileNameDest = "strings.xml";

		var pathBaseDestination = Directory.GetParent(Directory.GetParent(this.Host.ResolvePath("")).ToString()).ToString();

		var currentPathDest =pathBaseDestination + "/MyProject.App.AndroidApp/Resources/values-en/" + fileNameDest;

		using(var reader = new ResXResourceReader(fileResultRexPath))
		{
			reader.UseResXDataNodes = true;
			#>
			<resources>
			<#

					foreach(DictionaryEntry entry in reader)
					{
						var name = entry.Key;
						//if(!name.ToString().Contains("WEBSHOP_") && !name.ToString().Contains("DASHBOARD_"))//only include keys with these prefixes, or the country ones.
						//{
						//	if(name.ToString().Length != 2)
						//	{
						//		continue;
						//	}
						//}
						var node = (ResXDataNode)entry.Value;
						var value = node.GetValue((ITypeResolutionService) null); 
						 if (!String.IsNullOrEmpty(value.ToString())) value = value.ToString().Replace("\n", "");
						 if (!String.IsNullOrEmpty(value.ToString())) value = value.ToString().Replace("\r", "");
						 if (!String.IsNullOrEmpty(value.ToString())) value = value.ToString().Replace("&", "&amp;");
						 if (!String.IsNullOrEmpty(value.ToString())) value = value.ToString().Replace("<<", "");
						 //if (!String.IsNullOrEmpty(value.ToString())) value = value.ToString().Replace("'", "\'");
	#>
				  <string name="<#=name#>">"<#=value#>"</string>
	<#		
					}
	#>
				<string name="WEBSHOP_LASTELEMENT">just ignore this</string>
	<#
			#>
			</resources>
			<#
			File.Copy(fileResultPath, currentPathDest, true);
		}

	#>

Android works with `values-xx` folders, so above is for English for in the `values-en` folder. But you also have to generate a default which goes into the `values` folder. Just copy above T4 template and change the folder in the above code.

There you go, you can now use one single resource file for all your projects. This makes it very easy exporting everything to an excl document and let someone translate it and import it again.

Special thanks to [this amazing VS extension][12] which works awesome with `resx` files. Consider [donating][13] to him for his awesome work (I have nothing to do with that, I just love the extension).


  [1]: https://docs.microsoft.com/en-us/xamarin/xamarin-forms/app-fundamentals/localization/text?tabs=windows
  [2]: https://docs.microsoft.com/en-us/windows/uwp/app-resources/localize-strings-ui-manifest
  [3]: ReadMeImages/jqeVN.png
  [4]: ReadMeImages/Gl4IL.png
  [5]: ReadMeImages/uGOMH.png
  [6]: https://marketplace.visualstudio.com/items?itemName=TomEnglert.ResXManager
  [7]: http://www.ryadel.com/en/setup-a-multi-language-website-using-asp-net-mvc/
  [8]: https://stackoverflow.com/a/20419904/2901207
  [9]: http://t4-editor.tangible-engineering.com/T4-Editor-Visual-T4-Editing.html
  [10]: https://github.com/ngx-translate/core
  [11]: https://github.com/ngx-translate/core/issues/199#issuecomment-339084987
  [12]: https://github.com/tom-englert/ResXResourceManager
  [13]: https://www.paypal.com/cgi-bin/webscr?cmd=_s-xclick&hosted_button_id=TQQR8AKGNHELQ