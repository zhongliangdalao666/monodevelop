namespace MonoDevelop.FSharp

open System
open System.Xml
open System.CodeDom.Compiler
open System.IO

open MonoDevelop.Core
open MonoDevelop.Ide
open MonoDevelop.Ide.Gui.Content
open MonoDevelop.Projects
open Microsoft.FSharp.Compiler

type FSharpLanguageBinding() =
  static let LanguageName = "F#"

  let provider = lazy new CodeDom.FSharpCodeProvider()
  
  // ------------------------------------------------------------------------------------
  // Watch for changes that trigger a reparse 

  // Register handler that will reparse when the active configuration is changes
  do IdeApp.Workspace.ActiveConfigurationChanged.Add(fun _ -> 
         for doc in IdeApp.Workbench.Documents do
             if doc.Editor <> null then 
                doc.ReparseDocument ())

  // Register handler that will reparse when F# file is opened/closed (is this even needed?)
  do IdeApp.Workbench.ActiveDocumentChanged.Add(fun _ ->
    let doc = IdeApp.Workbench.ActiveDocument
    if doc <> null && (CompilerArguments.supportedExtension(IO.Path.GetExtension(doc.FileName.ToString()))) then
         doc.ReparseDocument())

    
  
  // ------------------------------------------------------------------------------------
  
  interface IDotNetLanguageBinding 
           with
    member x.BlockCommentEndTag = "*)"
    member x.BlockCommentStartTag = "(*"
    member x.Language = LanguageName
    member x.SingleLineCommentTag = "//"
    //member x.Parser = null
    //member x.Refactorer = null

    member x.GetFileName(baseName) = new FilePath(baseName.ToString() + ".fs")
    member x.IsSourceCodeFile(fileName) = CompilerArguments.supportedExtension (Path.GetExtension (fileName.ToString()))
    
    // IDotNetLanguageBinding
    override x.Compile(items, config, configSel, monitor) : BuildResult =
      CompilerService.Compile(items, config, configSel, monitor)

    override x.CreateCompilationParameters(options:XmlElement) : ConfigurationParameters =
      // Debug.tracef "Config" "Creating compiler configuration parameters"
      let pars = new FSharpCompilerParameters() 
      // Set up the default options
      if options <> null then 
          let debugAtt = options.GetAttribute ("DefineDebug")
          if (System.String.Compare ("True", debugAtt, StringComparison.OrdinalIgnoreCase) = 0) then
              pars.AddDefineSymbol "DEBUG"
              pars.DebugSymbols <- true
              pars.Optimize <- false
              pars.GenerateTailCalls <- false
          let releaseAtt = options.GetAttribute ("Release")
          if (System.String.Compare ("True", releaseAtt, StringComparison.OrdinalIgnoreCase) = 0) then
              pars.DebugSymbols <- true
              pars.Optimize <- true
              pars.GenerateTailCalls <- true

          // TODO: set up the documentation file to be AssemblyName.xml by default (but how do we get AssemblyName here?)
          //pars.DocumentationFile <- ""
          //    System.IO.Path.GetFileNameWithoutExtension(config.CompiledOutputName.ToString())+".xml" 
      pars :> ConfigurationParameters


    override x.CreateProjectParameters(options:XmlElement) : ProjectParameters =
      new FSharpProjectParameters() :> ProjectParameters
      
    override x.GetCodeDomProvider() : CodeDomProvider =
      null 
      // TODO: Simplify CodeDom provider to generate reasonable template
      // files at least for some MonoDevelop project types. Then we can recover:
      //   provider.Value :> CodeDomProvider
      
    override x.GetSupportedClrVersions() =
      [| ClrVersion.Net_2_0; ClrVersion.Net_4_0 |]

    override x.ProjectStockIcon = "md-fs-project"
