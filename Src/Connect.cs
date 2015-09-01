﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using EnvDTE;
using EnvDTE80;
using Extensibility;
using RT.Util;
using RT.Util.ExtensionMethods;

namespace RT.VisualStudio
{
    sealed class FontSupport
    {
        public string CommandName;
        public string FontName;
        public int FontSize;
        public bool UseBold = true;
    }

    /// <summary>The object for implementing an Add-in.</summary>
    /// <seealso class='IDTExtensibility2' />
    public class Connect : IDTExtensibility2, IDTCommandTarget
    {
        private FontSupport[] _fontsSupported = Ut.NewArray(
            new FontSupport { CommandName = "Cambria", FontName = "Cambria", FontSize = 12, UseBold = false },
            new FontSupport { CommandName = "Candara", FontName = "Candara", FontSize = 11 },
            new FontSupport { CommandName = "CourierNew", FontName = "Courier New", FontSize = 10 },
            new FontSupport { CommandName = "Georgia", FontName = "Georgia", FontSize = 11, UseBold = false },
            new FontSupport { CommandName = "MaiandraGD", FontName = "Maiandra GD kun Eo", FontSize = 11, UseBold = false },
            new FontSupport { CommandName = "OpenSans", FontName = "Open Sans", FontSize = 10, UseBold = false },
            new FontSupport { CommandName = "SegoeUI", FontName = "Segoe UI", FontSize = 11, UseBold = false }
        );

        private string[] _thingsToBold = new[] { "Keyword", "User Types", "User Types(Value types)", "User Types(Interfaces)", "User Types(Delegates)", "User Types(Enums)", "User Types(Type parameters)" };
        private string[] _platformPriorities = new[] { "x86", "Any CPU", "AnyCPU" };

        private DTE2 _applicationObject;
        private AddIn _addInInstance;
        private Dictionary<string, Action> _commands = new Dictionary<string, Action>();

        /// <summary>Implements the constructor for the Add-in object. Place your initialization code within this method.</summary>
        public Connect()
        {
        }

        /// <summary>Implements the OnConnection method of the IDTExtensibility2 interface. Receives notification that the Add-in is being loaded.</summary>
        /// <param term='application'>Root object of the host application.</param>
        /// <param term='connectMode'>Describes how the Add-in is being loaded.</param>
        /// <param term='addInInst'>Object representing this Add-in.</param>
        /// <seealso class='IDTExtensibility2' />
        public void OnConnection(object application, ext_ConnectMode connectMode, object addInInst, ref Array custom)
        {
            _applicationObject = (DTE2) application;
            _addInInstance = (AddIn) addInInst;

            if (connectMode == ext_ConnectMode.ext_cm_Startup)
            {
                CreateCommand("CloseAllToolWindows", "Close all Tool Windows", "Closes all tool windows.", () =>
                {
                    var windows = new List<Window>();
                    for (int i = 1; i <= _applicationObject.Windows.Count; i++)
                        if (_applicationObject.Windows.Item(i).Kind == "Tool")
                            windows.Add(_applicationObject.Windows.Item(i));
                    foreach (var window in windows)
                        window.Close();
                });

                foreach (var font in _fontsSupported)
                {
                    CreateCommand("ChangeFontTo" + font.CommandName, "Change Font to " + font.FontName, string.Format("Changes the text editor font to {0}.", font.FontName), () =>
                    {
                        foreach (Property prop in _applicationObject.Properties["FontsAndColors", "TextEditor"])
                        {
                            if (prop.Name == "FontFamily")
                                prop.Value = font.FontName;
                            else if (prop.Name == "FontSize")
                                prop.Value = font.FontSize;
                            else if (prop.Name == "FontsAndColorsItems")
                            {
                                var o = (FontsAndColorsItems) prop.Object;
                                foreach (ColorableItems obj in o)
                                    if (_thingsToBold.Contains(obj.Name))
                                    {
                                        obj.Bold = font.UseBold;
                                        obj.Background = 0x2000000;
                                    }
                            }
                        }
                    });
                }

                CreateCommand("ReformatXmlComments", "Reformat XML Comments", "Automatically word-wraps and reformats XML comments to conform to the RT comment style.", reformatComments);
            }
        }

        private void reformatComments()
        {
            var doc = (TextDocument) _applicationObject.ActiveDocument.Object();
            var startPoint = doc.CreateEditPoint();
            startPoint.StartOfDocument();
            var endPoint = doc.CreateEditPoint();
            endPoint.EndOfDocument();
            var source = startPoint.GetText(endPoint);
            var resultStr = CommentFormatter.ReformatComments(source);
            if (resultStr != source)
                startPoint.ReplaceText(endPoint, resultStr, (int) vsEPReplaceTextOptions.vsEPReplaceTextKeepMarkers);
        }

        private void CreateCommand(string commandName, string readableCommandName, string commandDescription, Action action)
        {
            try
            {
                object[] blah = { };
                ((Commands2) _applicationObject.Commands).AddNamedCommand2(_addInInstance, commandName, readableCommandName, commandDescription, true, Type.Missing, ref blah, 3, 3, vsCommandControlType.vsCommandControlTypeButton);
            }
            catch (ArgumentException)
            {
                // If we are here, then the exception is probably because a command with that name
                // already exists. If so there is no need to recreate the command and we can 
                // safely ignore the exception.
            }

            _commands[typeof(Connect).FullName + "." + commandName] = action;
        }

        /// <summary>Implements the OnDisconnection method of the IDTExtensibility2 interface. Receives notification that the Add-in is being unloaded.</summary>
        /// <param term='disconnectMode'>Describes how the Add-in is being unloaded.</param>
        /// <param term='custom'>Array of parameters that are host application specific.</param>
        /// <seealso class='IDTExtensibility2' />
        public void OnDisconnection(ext_DisconnectMode disconnectMode, ref Array custom)
        {
        }

        /// <summary>Implements the OnAddInsUpdate method of the IDTExtensibility2 interface. Receives notification when the collection of Add-ins has changed.</summary>
        /// <param term='custom'>Array of parameters that are host application specific.</param>
        /// <seealso class='IDTExtensibility2' />		
        public void OnAddInsUpdate(ref Array custom)
        {
        }

        /// <summary>Implements the OnStartupComplete method of the IDTExtensibility2 interface. Receives notification that the host application has completed loading.</summary>
        /// <param term='custom'>Array of parameters that are host application specific.</param>
        /// <seealso class='IDTExtensibility2' />
        public void OnStartupComplete(ref Array custom)
        {
        }

        /// <summary>Implements the OnBeginShutdown method of the IDTExtensibility2 interface. Receives notification that the host application is being unloaded.</summary>
        /// <param term='custom'>Array of parameters that are host application specific.</param>
        /// <seealso class='IDTExtensibility2' />
        public void OnBeginShutdown(ref Array custom)
        {
        }

        /// <summary>Implements the QueryStatus method of the IDTCommandTarget interface. This is called when the command's availability is updated</summary>
        /// <param term='commandName'>The name of the command to determine state for.</param>
        /// <param term='neededText'>Text that is needed for the command.</param>
        /// <param term='status'>The state of the command in the user interface.</param>
        /// <param term='commandText'>Text requested by the neededText parameter.</param>
        /// <seealso class='Exec' />
        public void QueryStatus(string commandName, vsCommandStatusTextWanted neededText, ref vsCommandStatus status, ref object commandText)
        {
            if (neededText == vsCommandStatusTextWanted.vsCommandStatusTextWantedNone)
            {
                if (_commands.ContainsKey(commandName))
                {
                    status = (vsCommandStatus) vsCommandStatus.vsCommandStatusSupported | vsCommandStatus.vsCommandStatusEnabled;
                    return;
                }
            }
        }

        /// <summary>Implements the Exec method of the IDTCommandTarget interface. This is called when the command is invoked.</summary>
        /// <param term='commandName'>The name of the command to execute.</param>
        /// <param term='executeOption'>Describes how the command should be run.</param>
        /// <param term='varIn'>Parameters passed from the caller to the command handler.</param>
        /// <param term='varOut'>Parameters passed from the command handler to the caller.</param>
        /// <param term='handled'>Informs the caller if the command was handled or not.</param>
        /// <seealso class='Exec' />
        public void Exec(string commandName, vsCommandExecOption executeOption, ref object varIn, ref object varOut, ref bool handled)
        {
            handled = false;
            if (executeOption == vsCommandExecOption.vsCommandExecOptionDoDefault)
            {
                if (_commands.ContainsKey(commandName))
                {
                    _commands[commandName]();
                    handled = true;
                }
            }
        }
    }
}
