
module Program

open System
open System.IO
open System.Text.RegularExpressions

open OpenQA.Selenium.Chrome
open OpenQA.Selenium.IE

open canopy
open reporters

open utils
open fileAccessFunctions

showInfoDiv <- false // added to remove the message inserted by canopy inside the browser

let getQaServerName (args : string []) =  // COQA-2270
    if args.Length = 7 then
        qaServerLst
        |> List.find(fun x -> x = args.[6])
    else defaultQaServerName

// DA: Uncomment for local debugging
//let showCanopyTests _ =
//    printfn "\nCanopy Tests to be run:"
//    canopy.runner.suites
//    |> List.iter (fun (s : suite) ->
//        s.Tests |> List.rev |> List.iteri (fun index t ->
//            printfn "test %i: %s" index t.Description))
//    printfn ""

[<EntryPoint>]
let main args =
    let automationStart = System.DateTime.Now
    if args.Length < 6 || args.Length > 7 then "Error: invalid number of arguments" |> CheckoutException |> raise
    let envArg = args.[0]
    let divisionArg, browserArg, runningTypeArg, fileNameArg, debugOptionArg = args.[1], args.[2], args.[3], args.[4], args.[5]
    let qaServerName = getQaServerName args
    match envArg with
    | "SendEmail" -> sendEmail.SendResults runningTypeArg
                     exit 0
    | _ -> Console.WriteLine(sprintf "continue with selenium scripts")

    // this does a case insensitive comparison of strings for the pattern matching of
    // the environment passed as the first parameter to the exe (active pattern match)
    /// active pattern for case insensitive string comparison
    let (|InvariantEqual|_|) (str:string) arg =
        if String.Compare(str, arg, StringComparison.OrdinalIgnoreCase) = 0
        then Some() else None

    let testBrowser = match browserArg with
                        | InvariantEqual "Chrome" -> Chrome
                        | InvariantEqual "IE" -> IE
                        | InvariantEqual "Firefox" -> Firefox
                        | _ -> failwith <| sprintf "invalid browser argument: %s. valid ones are: Chrome, IE or Firefox" browserArg

    if debugOptionArg = "teamcity" then
         reporter <- new TeamCityReporter() :> IReporter

    //environment
    let path = System.AppDomain.CurrentDomain.BaseDirectory

    let timestamp = utils.startDate()

    if debugOptionArg = "html" || debugOptionArg = "htm" then
        reporter <- new reporters2.LiveHtmlReporter(Chrome, path) :> IReporter
        let liveHtmlReporter = reporter :?> reporters2.LiveHtmlReporter
        liveHtmlReporter.reportPath <- Some ("C:\\logs2\\" + timestamp + "_" + runningTypeArg)

    Directory.CreateDirectory(@"C:\\logs7") |> ignore // for BigData Tests

    let reportName = @"..\..\..\DATA\REPORTS\Automation report " + envArg

    match testBrowser with
    | Chrome    -> let chromePath = "ChromeDriver.exe"
                   let mutable chromeProfilePath = System.IO.Path.Combine(path, "ChromeProfile")
                   chromeDir <- if System.IO.File.Exists(chromePath) then path else @"C:\"
                   chromeProfilePath <- if System.IO.Directory.Exists(chromeProfilePath) then chromeProfilePath else @"C:\SELENIUMPROFILE"
                   System.Console.WriteLine("Using chrome profile in: " + chromeProfilePath)
                   let options = new ChromeOptions()
                   options.AddArguments([|"log-path=c:\log\chromedriver.log"; "verbose"; "--disable-extensions"; "--disable-cache"; "user-data-dir=" + chromeProfilePath; "--start-maximized"|]);
                   let chromeWithOptions = ChromeWithOptions options
                   start chromeWithOptions 
    | IE        -> let iePath = System.IO.Path.Combine(path, "IEDriverServer.exe")
                   ieDir <- if System.IO.File.Exists(iePath) then path else @"C:\"
                   let options = new InternetExplorerOptions()
                   options.EnsureCleanSession <- true
                   //options.ForceCreateProcessApi <- true
                   //options.BrowserCommandLineArguments <- "-private"
                   start (IEWithOptions options)
    | firefox   -> start firefox

    try
        pin FullScreen
        Console.WriteLine(sprintf "_environment = <%s>" envArg)
        Console.WriteLine(sprintf "_division = <%s>" divisionArg)
        Console.WriteLine(sprintf "_lastRun = <%s>" (getCurrentTime @"dd-MM-yyyy HH:mm tt"))

        //start here
        match runningTypeArg with
        | "SmokeTest"                           -> SmokeTest.runSmokeTest envArg divisionArg
        | "WeeklyE2E"                           -> Verification.runWeeklyE2E envArg divisionArg

        | "GenerateReportSingleFile"            -> GenerateReports.generateReportsForDivision envArg fileNameArg divisionArg "_" ""
        | "GenerateMultipleReports"             -> GenerateReports.generateMultipleReports envArg fileNameArg divisionArg

        | "GenerateSegments"                    -> GenerateSegments.generateSegments envArg fileNameArg divisionArg
        | "GenerateMultipleSegments"            -> GenerateSegments.generateMultipleSegments envArg fileNameArg divisionArg
        | "CombineSegments"                     -> CombineSegments.combineSegments envArg fileNameArg divisionArg

        | "CreateGroups"                        -> CreateGroups.createGroupsFromSingleFile fileNameArg envArg divisionArg
        | "CreateMultipleGroups"                -> CreateGroups.createGroupsFromMultipleFiles fileNameArg envArg divisionArg
        | "DynamicStaticGroups"                 -> DynamicStaticGroups.runIT envArg divisionArg
        | "CompareGroups"                       -> CreateGroups.compareGroups envArg divisionArg fileNameArg

        | "ContextMenuStructure"                -> ContextMenu.contextMenuStructure envArg divisionArg
        | "LocationStructure"                   -> LiquorLocationStructure.liquorLocationStructureTest envArg

        | "ReportSpecific"                      -> ReportSpecificTests.runner envArg divisionArg fileNameArg
        | "SuppressionMetrics"                  -> SuppressionMetrics.runner envArg divisionArg fileNameArg
        | "RenameReport"                        -> RenameReport.runner envArg divisionArg
        | "EditRerunReport"                     -> EditRerunReport.runner envArg divisionArg

        | "UserPermissions5"                    -> UserPermissions5.userPermissions5 envArg divisionArg fileNameArg
        | "UserPermissions9"                    -> UserPermissions.userPermissions9 envArg

        | "QMSearch"                            -> QMSearch.runner envArg divisionArg
        | "QMQuickMetrics"                      -> QMQuickMetrics.runner envArg divisionArg
        | "QMDriverTree"                        -> QMDriverTree.runner envArg divisionArg
        | "QMSegBreakdown"                      -> QMSegBreakdown.runner envArg divisionArg
        | "QMGrowthSales"                       -> QMGrowthSales.runner envArg divisionArg
        | "QMDynamicRules"                      -> QMDynamicRules.runner envArg divisionArg
        | "QMProdAttr"                          -> QMProdAttr.runner envArg divisionArg
        | "QMUserPermissions"                   -> QMUserPermissions.runner envArg divisionArg
        | "QMKDTReport"                         -> QMOtherTests.qmKDTReportCheck envArg
        | "QMFreeTrial"                         -> QMFreeTrial.runner envArg
        | "QMPanelPersist"                      -> QMOtherTests.qmPanelPersistenceCheck envArg
        | "QMBrowserHistory"                    -> QMBrowserHistory.runner envArg divisionArg
        | "QMFeedback"                          -> QMFeedback.runner envArg divisionArg

        | "DSHCarousel"                         -> DSHCarousel.runner envArg divisionArg
        | "CCSWeeklyE2E"                        -> CBG.runWeeklyE2ECBGtests envArg divisionArg

        | "SandBox"                             -> sandBox.runner envArg divisionArg fileNameArg

        | "Favourites"                          -> FavouriteShare.loginAndFavourite envArg divisionArg
        | "PerformanceReport"                   -> PerformanceReport.performanceReport envArg fileNameArg
        | "LoadTestReport"                      -> LoadTestReport.loadTestReport envArg fileNameArg

        | "BigData"                             -> BigData.runner envArg divisionArg fileNameArg
        | "CheckAllReports"                     -> CheckReports.checkAllReports fileNameArg envArg divisionArg
        | "CompareWeeklyData"                   -> RegressionAutomation.compareWeeklyData divisionArg qaServerName
        | "ExportAll"                           -> Export.runner envArg divisionArg "_" false true
        | "ExportAllOnlyExcel"                  -> Export.runner envArg divisionArg fileNameArg true true
        | "ExportPerReport"                     -> Export.runner envArg divisionArg fileNameArg false true

        | "BuildReports"                        -> BuildReports.runner fileNameArg
        | "RegressionAutomation"                -> RegressionAutomation.runner envArg divisionArg fileNameArg
        | "ConvertReportToXML"                  -> ConvertReportToXML.runner envArg fileNameArg

        | "CleanAccounts"                       -> CleanAccounts.runner envArg divisionArg fileNameArg

        | "AdminFiles"                          -> AdminFilesUpload.runner envArg
        | "PerformanceTest"                     -> LoadTest.runner envArg fileNameArg true
        | "LoadTest"                            -> LoadTest.runner envArg fileNameArg false
        | "LoadTestWaitForFinish"               -> LoadTest.waitForFinish envArg fileNameArg |> ignore
        | "LoadTestBenchmarking"                -> LoadTestBenchmarking.runner fileNameArg
        | "BanchmarkTwoVersions"                -> LoadTestBenchmarking.compareTwoVersions divisionArg fileNameArg

        | "oOhPerformance"                      -> oOhPerformance.measureoOhPerformance envArg fileNameArg
        | "oOhReport"                           -> oOhReport.generateReport envArg
        | "oOhIntegration"                      -> oOhIntegration.integrationTests envArg

        | _                                     -> describe "METHOD IS NOT IMPLEMENTED"

        //showCanopyTests()  // DA: Uncomment for local debugging
        run()

    finally
        let automationEnd = System.DateTime.Now - automationStart
        let mutable enVersion = "3.0"
        if browserArg <> "IE" && runningTypeArg.Contains("LoginPagePing") = false then enVersion <- Login.version envArg //To increase E2EDaily speed
        // BO: Added try with clauses here because very rarely it happened that there was an error while QUITTING chromedriver that would mark tests as failed. 
        // BO: Issue happened only 2 times in over 10k tests, but reporting is also used outside of the team, and are disturbing their work for no reason.
        try quit()
        with ex -> Console.WriteLine("FAILURE while closing the driver!")
                   executeCMD "taskkill /f /im chromedriver.exe" |> ignore
                   executeCMD "taskkill /f /im chrome.exe" |> ignore
                   Console.Write(ex)
        try
            if debugOptionArg = "html" || debugOptionArg = "htm" then
                let reportFileName = sprintf "%s_%s_%s" timestamp runningTypeArg fileNameArg |> fun s -> Regex.Replace(s,"\?|\||\/","!")
                                     |> fun s -> if s.Length > 200 then s.Substring(0,200) else s
                                     |> fun s -> s + sprintf "_%s.%s" enVersion debugOptionArg
                if runningTypeArg = "BigData" then File.Move("C:\\logs2\\" + timestamp + "_" + runningTypeArg + ".html","C:\\logs7\\" + reportFileName)
                elif debugOptionArg = "htm" && reporters2.failure = false then
                    File.Delete("C:\\logs2\\" + timestamp + "_" + runningTypeArg + ".html")
                else File.Move("C:\\logs2\\" + timestamp + "_" + runningTypeArg + ".html", "C:\\logs2\\" + reportFileName)
                let testResultUrl = "http://" + qaServerName + "/" + reportFileName.Replace(" ", "%20").Trim()
                Console.WriteLine("For test results, please go to " + testResultUrl)
                if not (isRunningLocally()) then
                    sendMessageToSlack.sendMessageToSlack runningTypeArg reporters2.failure testResultUrl
        with ex -> Console.WriteLine("FAILURE while renaming test report files: ")
                   Console.Write(ex)

        if(debugOptionArg = "debug")then
            // waits for enter from keyboard in console
            printfn "press [enter] to exit"
            System.Console.ReadLine() |> ignore

        if runningTypeArg.Contains("Export") || runningTypeArg.Contains("Regression") || runningTypeArg = "QuickMetrics" || runningTypeArg.Contains("QM") || runningTypeArg = "UserPermissions5" then
            let localDownloadsFolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "\\Downloads\\"
            let qaServerFolder = @"\\" + qaServerName + @"\Weekly\E2E_" + enVersion
            Directory.CreateDirectory(qaServerFolder) |> ignore
            getLatestFiles()
            |> Seq.iter(fun file ->
                if file.Contains("=") then
                    let start =
                        let charsLength = (localDownloadsFolder + qaServerFolder + "\\" + file).Length
                        if charsLength < 259 then 0
                        else charsLength - 258
                    try File.Move(localDownloadsFolder + file, qaServerFolder + "\\" + file.[start..]) // for long file names to ensure less than 260 chars in file path and name
                    with | _ -> Console.WriteLine("File " + file + " cannot be moved.")
            )
            getAllFilesFromPath "C:\\tmp"
            |> Seq.iter(fun f ->
                if f.EndsWith(".png") then
                    try File.Move(@"C:\tmp\" + f, @"\\" + qaServerName + @"\Weekly\Screenshots\" + System.Environment.MachineName + "_" + f)
                    with | _ -> Console.WriteLine("File " + f + " cannot be moved.")
            )
        Console.WriteLine("Elapsed time: " + automationEnd.ToString())

    if debugOptionArg = "html" || debugOptionArg = "htm" then
        if reporters2.failure then -1 else 0
    else 0