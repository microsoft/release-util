using McMaster.Extensions.CommandLineUtils;
using Microsoft.VisualStudio.Services.ReleaseManagement.WebApi;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Tapas.CICD.ReleaseHelper;

namespace Microsoft.Sample.ReleaseUtil
{
    class Program
    {
        static void Main(string[] args)
        {
            // Instantiate the command line app
            var app = new CommandLineApplication();

            // Command line info used by help
            app.Name = "ReleaseUtil";
            app.Description = "Command line util that utilizes the ReleaseHelper library.";
            app.ExtendedHelpText = "This is a command line util to help streamline the process of getting information and errors about releases."
                + Environment.NewLine + "It utilizes the ReleaseHelper library.  The application can be executed by running the ReleaseUtil.exe.";

            // Set the arguments to display the description and help text
            app.HelpOption("-?|-h|--help");

            // This is a helper/shortcut method to display version info
            // The default help text is "Show version Information"
            app.VersionOption("-v|--version", () => {
                return Assembly.GetEntryAssembly().GetName().Version.ToString();
            });

            // When no commands are specified, this block will execute
            app.OnExecute(() =>
            {
                app.ShowHint();
                return 0;
            });

            // Errors command to show release errors
            app.Command("errors", (command) =>
            {
                command.ExtendedHelpText = "The errors command can be used to return error information experienced by releases.";
                command.Description = "Errors command used to return Azure Devops/TFS release errors.";
                command.HelpOption("-?|-h|--help");

                // Project collection url options: REQUIRED
                var optionProjectCollectionUrl = command.Option("-p|--project-collection-url",
                    "Required - project collection url",
                    CommandOptionType.MultipleValue).IsRequired();

                // Project name options: REQUIRED
                var optionProjectName = command.Option("-n|--project-name",
                    "Required - project name",
                    CommandOptionType.MultipleValue).IsRequired();

                // Release definition name options: REQUIRED
                var optionReleaseDefinitionName = command.Option("-r|--release-definition-name",
                    "Required - release definition name",
                    CommandOptionType.SingleValue).IsRequired();

                /*
                // Personal access token options
                var optionPersonalAccessToken = command.Option("-pat|--personal-access-token",
                    "When provided, will authenticate utilizing a token instead of prompting for user credentials",
                    CommandOptionType.SingleValue).IsRequired();
                */

                // Top count options
                var optionTopCount = command.Option("-t|--top",
                    "Top count option to specify how many errors to return",
                    CommandOptionType.SingleValue);

                // Date range options
                var optionDateRange = command.Option("-d|--date-range",
                    "Ability to filter by providing a date range.  Must provide both a start and end date",
                    CommandOptionType.MultipleValue);

                // Release name options
                var optionReleaseName = command.Option("-rn|--release-name",
                    "Ability to filter by release name",
                    CommandOptionType.SingleValue);

                // Environment name options
                var optionEnvironmentName = command.Option("-e|--environment-name",
                    "Ability to filter by environment name",
                    CommandOptionType.SingleValue);


                command.OnExecute(() =>
                {
                    //Default top count to 100 and update it if top count option provided
                    int topcount = 100;
                    if (optionTopCount.HasValue())
                        topcount = Convert.ToInt32(optionTopCount.Value());

                    // These are all required options for command and we can assume they all
                    // have values as the command would stop if one was not provided
                    string optionProjectCollectionUrlValue = optionProjectCollectionUrl.Value();
                    string optionProjectNameValue = optionProjectName.Value();
                    string optionReleaseDefinitionNameValue = optionReleaseDefinitionName.Value();

                    TfsInfo info = new TfsInfo()
                    {
                        ProjectCollectionUrl = optionProjectCollectionUrlValue,
                        ProjectName = optionProjectNameValue,
                        ReleaseDefinitionName = optionReleaseDefinitionNameValue
                    };

                    using (TfsRelease tfsRelease = new TfsRelease(info))
                    {
                        string result = "";

                        //Date Range option passed in
                        if (optionDateRange.HasValue())
                        {
                            List<string> optionDateRangeValues = optionDateRange.Values;
                            if (optionDateRangeValues.Count > 1)
                            {
                                DateTime startdate = new DateTime();
                                DateTime enddate = new DateTime();
                                if (DateTime.TryParse(optionDateRangeValues[0], out startdate))
                                {
                                    if (DateTime.TryParse(optionDateRangeValues[1], out enddate))
                                    {
                                        result = tfsRelease.GetDeploymentErrors(startdate, enddate);

                                        if (!result.StartsWith($"**Warning**"))
                                        {
                                            JArray obj = JArray.Parse(result);
                                            result = JsonConvert.SerializeObject(obj.Take(topcount), Formatting.Indented);
                                        }
                                    }
                                    else { result = $"**Warning** Invalid end date supplied to date range option."; }
                                }
                                else { result = $"**Warning** Invalid start date supplied to date range option."; }
                            }
                            else { result = $"**Warning** Must provide two dates when using the date range option."; }
                        }
                        else
                        {
                            result = tfsRelease.GetDeploymentErrors(topcount);
                        }

                        //Check to see if any warnings returned
                        if (!result.StartsWith($"**Warning**"))
                        {
                            //Release Name option passed in
                            if (optionReleaseName.HasValue())
                            {
                                JArray obj = JArray.Parse(result);
                                result = JsonConvert.SerializeObject(obj.Children().Where(e => e.Value<string>("ReleaseName") == optionReleaseName.Value()).ToList(), Formatting.Indented);
                            }

                            //Environment Name option passed in
                            if (optionEnvironmentName.HasValue())
                            {
                                JArray obj = JArray.Parse(result);
                                result = JsonConvert.SerializeObject(obj.Children().Where(e => e.Value<string>("EnvironmentName") == optionEnvironmentName.Value()).ToList(), Formatting.Indented);
                            }
                        }

                        Console.WriteLine(result);
                    };

                    return 0; // return 0 on a successful execution
                });
            });

            // Stats command to show deployment stats
            app.Command("stats", (command) =>
            {
                command.ExtendedHelpText = "The stats command can be used to return stats information about release deployments.";
                command.Description = "Stats command used to return Azure Devops/TFS release stats.";
                command.HelpOption("-?|-h|--help");

                // Project collection url options: REQUIRED
                var optionProjectCollectionUrl = command.Option("-p|--project-collection-url",
                    "Required - project collection url",
                    CommandOptionType.MultipleValue).IsRequired();

                // Project name options: REQUIRED
                var optionProjectName = command.Option("-n|--project-name",
                    "Required - project name",
                    CommandOptionType.MultipleValue).IsRequired();

                // Release definition name options: REQUIRED
                var optionReleaseDefinitionName = command.Option("-r|--release-definition-name",
                    "Required - release definition name",
                    CommandOptionType.SingleValue).IsRequired();

                /*
                // Personal access token options
                var optionPersonalAccessToken = command.Option("-pat|--personal-access-token",
                    "When provided, will authenticate utilizing a token instead of prompting for user credentials",
                    CommandOptionType.SingleValue).IsRequired();
                */

                // Top count options
                var optionTopCount = command.Option("-t|--top",
                    "Top count option to specify how many errors to return",
                    CommandOptionType.SingleValue);

                // Date range options
                var optionDateRange = command.Option("-d|--date-range",
                    "Ability to filter by providing a date range.  Must provide both a start and end date",
                    CommandOptionType.MultipleValue);

                // Release name options
                var optionReleaseName = command.Option("-rn|--release-name",
                    "Ability to filter by release name",
                    CommandOptionType.SingleValue);

                // Environment name options
                var optionEnvironmentName = command.Option("-e|--environment-name",
                    "Ability to filter by environment name",
                    CommandOptionType.SingleValue);


                command.OnExecute(() =>
                {
                    //Default top count to 100 and update it if top count option provided
                    int topcount = 100;
                    if (optionTopCount.HasValue())
                        topcount = Convert.ToInt32(optionTopCount.Value());

                    // These are all required options for command and we can assume they all
                    // have values as the command would stop if one was not provided
                    string optionProjectCollectionUrlValue = optionProjectCollectionUrl.Value();
                    string optionProjectNameValue = optionProjectName.Value();
                    string optionReleaseDefinitionNameValue = optionReleaseDefinitionName.Value();

                    TfsInfo info = new TfsInfo()
                    {
                        ProjectCollectionUrl = optionProjectCollectionUrlValue,
                        ProjectName = optionProjectNameValue,
                        ReleaseDefinitionName = optionReleaseDefinitionNameValue
                    };

                    using (TfsRelease tfsRelease = new TfsRelease(info))
                    {
                        string result = "";

                        //Date Range option passed in
                        if (optionDateRange.HasValue())
                        {
                            List<string> optionDateRangeValues = optionDateRange.Values;
                            if (optionDateRangeValues.Count > 1)
                            {
                                DateTime startdate = new DateTime();
                                DateTime enddate = new DateTime();
                                if (DateTime.TryParse(optionDateRangeValues[0], out startdate))
                                {
                                    if (DateTime.TryParse(optionDateRangeValues[1], out enddate))
                                    {
                                        result = tfsRelease.GetDeploymentStats(startdate, enddate);

                                        if (!result.StartsWith($"**Warning**"))
                                        {
                                            JArray obj = JArray.Parse(result);
                                            result = JsonConvert.SerializeObject(obj.Take(topcount), Formatting.Indented);
                                        }
                                    }
                                    else { result = $"**Warning** Invalid end date supplied to date range option."; }
                                }
                                else { result = $"**Warning** Invalid start date supplied to date range option."; }
                            }
                            else { result = $"**Warning** Must provide two dates when using the date range option."; }
                        }
                        else
                        {
                            result = tfsRelease.GetDeploymentStats(topcount);
                        }

                        //Check to see if any warnings returned
                        if (!result.StartsWith($"**Warning**"))
                        {
                            JArray obj = JArray.Parse(result);
                            var deploymentlist = obj.ToObject<List<TfsDeploymentInfo>>();

                            //Release Name option passed in
                            if (optionReleaseName.HasValue())
                            {
                                deploymentlist = deploymentlist.Where(e => e.ReleaseName == optionReleaseName.Value()).ToList();
                            }

                            //Environment Name option passed in
                            if (optionEnvironmentName.HasValue())
                            {
                                deploymentlist = deploymentlist.Where(e => e.EnvironmentName == optionEnvironmentName.Value()).ToList();
                            }

                            var deploymentscombined = deploymentlist.GroupBy(e => e.StartedOn.Date).Select(
                                g => new
                                {
                                    Date = (g.Key.Year != 0001 ? g.Key.ToString("MM-dd-yyyy") : "Not started"),
                                    Undefined = g.Count(s => s.Status == DeploymentStatus.Undefined),
                                    NotDeployed = g.Count(s => s.Status == DeploymentStatus.NotDeployed),
                                    InProgress = g.Count(s => s.Status == DeploymentStatus.InProgress),
                                    Succeeded = g.Count(s => s.Status == DeploymentStatus.Succeeded),
                                    PartiallySucceeded = g.Count(s => s.Status == DeploymentStatus.PartiallySucceeded),
                                    Failed = g.Count(s => s.Status == DeploymentStatus.Failed),
                                    TimeTaken = new TimeSpan(g.Sum(s => s.TimeTaken.Ticks)).ToString(@"dd\.hh\:mm\:ss")
                                }).OrderBy(e => e.Date);

                            //Concat in totals row
                            var deploymentstats = deploymentscombined.Concat(
                                new[]{new
                                {
                                    Date ="Total",
                                    Undefined = deploymentlist.Count(s => s.Status == DeploymentStatus.Undefined),
                                    NotDeployed = deploymentlist.Count(s => s.Status == DeploymentStatus.NotDeployed),
                                    InProgress = deploymentlist.Count(s => s.Status == DeploymentStatus.InProgress),
                                    Succeeded = deploymentlist.Count(s => s.Status == DeploymentStatus.Succeeded),
                                    PartiallySucceeded = deploymentlist.Count(s => s.Status == DeploymentStatus.PartiallySucceeded),
                                    Failed = deploymentlist.Count(s => s.Status == DeploymentStatus.Failed),
                                    TimeTaken = new TimeSpan(deploymentlist.Sum(s => s.TimeTaken.Ticks)).ToString(@"dd\.hh\:mm\:ss")
                                } });

                            result = JsonConvert.SerializeObject(deploymentstats, Formatting.Indented);
                        }

                        Console.WriteLine(result);
                    };

                    return 0; // return 0 on a successful execution
                });
            });

            try
            {
                // This begins the actual execution of the application
                app.Execute(args);
            }
            catch (CommandParsingException ex)
            {
                // You'll always want to catch this exception, otherwise it will generate a messy and confusing error for the end user.
                // the message will usually be something like:
                // "Unrecognized command or argument '<invalid-command>'"
                Console.WriteLine(ex.Message);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Unable to execute application: {0}", ex.Message);
            }
        }
    }
}
