using System.Text.RegularExpressions;
using System;
using System.IO;
using System.Text;
using System.Xml;
using Fonet;
using iText.Kernel.Pdf;
using PowerArgs;
using Serilog;
using Serilog.Events;
using iText.Kernel.Geom;
using iText.Layout;
using iText.Kernel.Font;
using iText.IO.Font;
using iText.Layout.Element;
using iText.IO.Image;
using iText.Kernel.Pdf.Canvas;
using iText.Kernel.Pdf.Extgstate;
using System.Net;

namespace Microcelium.CoverLetter
{
  [ArgExceptionBehavior(ArgExceptionPolicy.StandardExceptionHandling)]
  public class Host
  {
    private static readonly Regex nonWordRegex = new Regex("[^_a-z0-9]", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private string dd;

    [HelpHook]
    [ArgShortcut("-?")]
    public bool Help { get; set; }

    [ArgRequired]
    [ArgShortcut("-t")]
    public string Title { get; set; }

    [ArgRequired]
    [ArgShortcut("-o")]
    public string Organization { get; set; }

    [ArgShortcut("-l")]
    public string Logo { get; set; }

    [ArgShortcut("-v")]
    [DefaultValue("false")]
    public bool Verbose { get; set; }

    [ArgShortcut("p")]
    [DefaultValue("false")]
    public bool PlainText { get; set; }

    /// <summary>
    ///   Cleans up the organization name to create a directory
    /// </summary>
    private string OrganizationDirectory => nonWordRegex.Replace(Organization, string.Empty);

    /// <summary>
    ///   Has a logo argument been provided
    /// </summary>
    private bool HasLogo => !string.IsNullOrEmpty(Logo);

    /// <summary>
    ///   Don't want to company name in the cover letter file name, so we organize
    ///   them by directory
    /// </summary>
    private string DestinationDirectory
    {
      get
      {
        if (!string.IsNullOrEmpty(dd))
          return dd;

        var dest = Environment.GetEnvironmentVariable("JobHuntBaseDirectory");
        if (string.IsNullOrEmpty(dest))
        {
          Log.Debug($"Global variable 'JobHuntBaseDirectory' not set, using current directory");
          dest = Environment.CurrentDirectory;
        }

        Log.Debug($"Persisting to directory '{dest}'");
        dd = System.IO.Path.Combine(dest, OrganizationDirectory);
        if (!Directory.Exists(dd))
        {
          Log.Debug($"Creating Directory '{dd}'");
          Directory.CreateDirectory(dd);
        }

        return dd;
      }
    }

    private string DestinationPath => System.IO.Path.Combine(
      DestinationDirectory,
      $"{DateTime.Today:yyyy-MM-dd}-JoeGarro-CoverLetter.pdf");

    public void Main()
    {
      Log.Logger = new LoggerConfiguration()
        .Enrich.WithProperty("app", "microcelium-coverletter")
        .Enrich.FromLogContext()
        .MinimumLevel.Is(Verbose ? LogEventLevel.Debug : LogEventLevel.Warning)
        .WriteTo.Console()
        .CreateLogger();

      if (PlainText)
      {
        bool first = true;
        int i = 0;

        for (; i < Parts.Length; i++, first = false)
        {
          if (!first) Console.WriteLine();
          Console.WriteLine(Parts[i]);
        }

        return;
      }

      Log.Warning($"Writing file '{DestinationPath}'");
      var pdf = new PdfDocument(
        new PdfWriter(
          DestinationPath,
          new WriterProperties().AddUAXmpMetadata().SetPdfVersion(PdfVersion.PDF_1_7)));

      var doc = new Document(pdf, PageSize.LETTER);
      var catalog = pdf.GetCatalog();
      catalog.SetViewerPreferences(new PdfViewerPreferences().SetDisplayDocTitle(true));
      catalog.SetLang(new PdfString("en-US"));
      pdf.GetDocumentInfo().SetTitle($"Cover Letter - Joseph Garro - {DateTime.Today:yyyy-MM-dd}");

      ApplyCompanyLogo(pdf);
      ApplyText(doc);

      doc.Close();
    }

    private void ApplyCompanyLogo(PdfDocument pdf)
    {
      if (!HasLogo)
        return;

      var logoPath = System.IO.Path.Combine(DestinationDirectory, "logo.img");
      Log.Debug($"Downloading Logo to: '{logoPath}'");
      new WebClient().DownloadFile(Logo, logoPath);

      var pageSize = PageSize.LETTER;
      var image = ImageDataFactory.Create(logoPath);

      var canvas = new PdfCanvas(pdf.AddNewPage());
      var state = new PdfExtGState().SetFillOpacity(0.25f);

      var pageWidth = pageSize.GetWidth();
      var pageHeight = pageSize.GetHeight();
      image.SetWidth(Math.Min(pageWidth * 0.9f, image.GetWidth()));
      var xoffset = (pageWidth - image.GetWidth()) / 2;
      var yoffset = (pageHeight - image.GetHeight() - 96);

      canvas.SaveState();
      canvas.SetExtGState(state);
      canvas.AddImage(image, xoffset, yoffset, image.GetWidth(), false);
      canvas.RestoreState();
    }

    private void ApplyText(Document doc)
    {
      var font = PdfFontFactory.CreateFont();

      foreach (var part in Parts)
      {
        var p = new Paragraph();
        p.SetFont(font);
        p.Add(part);
        doc.Add(p);
      }
    }

    private string[] Parts => new[]
    {
      "To Whom It May Concern:",
      $"I would appreciate the opportunity to discuss the {Title} position available at {Organization}. "
      + "I feel my experience and interests are an excellent match for this position.",
      "I am intimately familiar with the .NET ecosystem from the front-end to the back-end and all "
      + "the middle-ware in between. I have converted AngularJS projects to Angular backed by .NET WebAPI. "
      + "I have migrated entire solutions from on-premise to the cloud. I have a deep understanding of "
      + "the nooks and crannies of SQL Server including the data architecture's affect on disk and memory "
      + "utilization and consequently performance. Leading DevOps I developed and managed an "
      + "Octopus Deploy solution exceeding 60 EC2 instances and over 30 Octopus projects. Over 50 mission "
      + "critical project build scripts were implemented with FAKE (F# Make) and consistently capable of "
      + "producing simple developer local builds to production ready TeamCity release builds.",
      "I believe I am an excellent fit for this position and look forward to learning more about this opportunity.",
      $"Sincerely,{Environment.NewLine}Joe Garro",
      $"#1.541.690.0641{Environment.NewLine}"
      + $"joe@microcelium.net{Environment.NewLine}"
      + "https://www.microcelium.net"
    };
  }
}
