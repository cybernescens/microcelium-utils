#r "paket:
nuget JetBrains.dotCover.CommandLineTools
nuget AWSSDK.S3
nuget Fake.BuildServer.TeamCity
nuget Fake.Core.Xml
nuget Fake.Core.Target
nuget Fake.Core.Trace
nuget Fake.DotNet.Cli
nuget Fake.DotNet.MSBuild
nuget Fake.DotNet.NuGet
nuget Fake.IO.FileSystem
nuget Fake.IO.Zip
nuget Fake.Runtime
//"
#load ".microcelium/lib/microcelium.fsx"
#load ".fake/build.fsx/intellisense.fsx"
open Fake.Core
open Fake.BuildServer
open Fake.DotNet
open Fake.IO
open Fake.IO.FileSystemOperators
open Fake.IO.Globbing.Operators
open Fake.Core.TargetOperators
open Microcelium.Fake

BuildServer.install [ TeamCity.Installer ]

if BuildServer.buildServer <> TeamCity then
  CoreTracing.ensureConsoleListener ()

let version = Version.from "1.0"
let versionparts = Version.parts version
let versionstr = Version.toString version

let srcDir = Path.getFullName "./src"
let binDir = Path.getFullName "./bin"

let srcDirCoverLetter = srcDir @@ "coverletter"

let project = "microcelium-utils"
let tests = seq { yield (srcDir, Default) }

Target.create "Clean" <| Targets.clean srcDir binDir
Target.create "Version" <| Targets.version version
Target.create "Build" ignore
Target.create "Test" <| ignore
Target.create "Publish" <| Targets.publish binDir
Target.create "Package" ignore

(* about the only part that needs customized *)

Target.create "BuildCoverLetter" <| Targets.build srcDirCoverLetter versionparts None

Target.create "PackageCoverLetter" (fun _ ->
  Build.packageNuget srcDirCoverLetter "microcelium-coverletter" versionparts binDir |> ignore
)

Target.create "ToLocalNuget"  <| Targets.publishLocal binDir versionstr

(* `NuGetCachePath` EnvVar should be set to your Nuget Packages Install dir already, but
    `TargetVersion` should be set prior to running build.bat :
    set TargetVersion=1.14 *)
Target.create "ToLocalPackageRepo" <| Targets.packageLocal srcDir

"BuildCoverLetter" ==> "Build"
"PackageCoverLetter" ==> "Package"

"Clean"
  ==> "Version"
  ==> "Build"
  ==> "Test"
  ==> "Package"
  =?> ("Publish", Environment.runPublish)

Target.runOrDefault <| if Environment.runPublish then "Publish" else "Test"
