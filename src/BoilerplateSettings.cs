﻿using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Settings;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace UnitTestBoilerplate
{
	public class BoilerplateSettings : IBoilerplateSettings
	{
		private const string CollectionPath = "UnitTestBoilerplateGenerator";

		private const string TestProjectsKey = "TestProjectsDictionary";

		private readonly WritableSettingsStore Store;

		// Key is the solution path and value is the last used unit test project for the solution.
		private readonly Dictionary<string, string> TestProjectsDictionary;

		public BoilerplateSettings()
		{
			SettingsManager settingsManager = new ShellSettingsManager(ServiceProvider.GlobalProvider);
			Store = settingsManager.GetWritableSettingsStore(SettingsScope.UserSettings);
			Store.CreateCollection(CollectionPath);

			if (Store.PropertyExists(CollectionPath, TestProjectsKey))
			{
				string dictionaryString = Store.GetString(CollectionPath, TestProjectsKey);

				if (string.IsNullOrEmpty(dictionaryString))
				{
					TestProjectsDictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
				}
				else
				{
					TestProjectsDictionary = new Dictionary<string, string>(JsonConvert.DeserializeObject<Dictionary<string, string>>(dictionaryString), StringComparer.OrdinalIgnoreCase);
				}
			}
			else
			{
				TestProjectsDictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			}
		}

		public void SaveSelectedTestProject(string solutionPath, string testProjectPath)
		{
			TestProjectsDictionary[solutionPath] = testProjectPath;
			Store.SetString(CollectionPath, TestProjectsKey, JsonConvert.SerializeObject(TestProjectsDictionary));
		}

		public string GetLastSelectedProject(string solutionPath)
		{
			string result;
			if (TestProjectsDictionary.TryGetValue(solutionPath, out result))
			{
				return result;
			}

			return null;
		}

		public string GetTemplate(MockFramework mockFramework, TemplateType templateType)
		{
			string templateSettingKey = GetTemplateSettingsKey(mockFramework, templateType);

			if (Store.PropertyExists(CollectionPath, templateSettingKey))
			{
				return Store.GetString(CollectionPath, GetTemplateSettingsKey(mockFramework, templateType));
			}

			return GetDefaultTemplate(mockFramework, templateType);
		}

		public string GetDefaultTemplate(MockFramework mockFramework, TemplateType templateType)
		{
			// When looking for default templates, redirect "Unknown" to "Moq"
			if (mockFramework == MockFramework.Unknown)
			{
				mockFramework = MockFramework.Moq;
			}

			using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream($"UnitTestBoilerplate.DefaultTemplates.{mockFramework}.{templateType}.txt"))
			{
				if (stream == null)
				{
					return string.Empty;
				}

				using (var reader = new StreamReader(stream))
				{
					return reader.ReadToEnd();
				}
			}
		}

		public void SetTemplate(MockFramework mockFramework, TemplateType templateType, string template)
		{
			Store.SetString(CollectionPath, GetTemplateSettingsKey(mockFramework, templateType), template);
		}

		public void SetTemplate(string mockFrameworkString, string templateTypeString, string template)
		{
			Store.SetString(CollectionPath, GetTemplateSettingsKey(mockFrameworkString, templateTypeString), template);
		}

		public void RevertTemplateToDefault(MockFramework mockFramework)
		{
			foreach (TemplateType templateType in Enum.GetValues(typeof(TemplateType)))
			{
				string templateSettingKey = GetTemplateSettingsKey(mockFramework, templateType);
				if (Store.PropertyExists(CollectionPath, templateSettingKey))
				{
					Store.DeleteProperty(CollectionPath, templateSettingKey);
				}
			}
		}

		private static string GetTemplateSettingsKey(MockFramework mockFramework, TemplateType templateType)
		{
			return GetTemplateSettingsKey(mockFramework.ToString(), templateType.ToString());
		}

		private static string GetTemplateSettingsKey(string mockFrameworkString, string templateTypeString)
		{
			return $"Template_{mockFrameworkString}_{templateTypeString}";
		}
	}
}
