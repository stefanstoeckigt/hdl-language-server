﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using JsonRpc;
using Lsp;
using Lsp.Capabilities.Client;
using Lsp.Capabilities.Server;
using Lsp.Models;
using Lsp.Protocol;
using VHDL;
using VHDL.parser;
using System.IO;
using VHDL.ParseError;
using System.Threading;

namespace SampleServer
{
    class Program
    {

        static void Main(string[] args)
        {
            MainAsync(args).Wait();
        }

        static async Task MainAsync(string[] args)
        {
            //while (!System.Diagnostics.Debugger.IsAttached)
            //{
            //    await Task.Delay(100);
            //}
            var ipEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 5001);

            var listener = new TcpListener(ipEndPoint);
            listener.Server.LingerState = new LingerOption(true, 10);
            listener.Start();

            var client = await listener.AcceptTcpClientAsync();

            var server = new LanguageServer(client.GetStream(), client.GetStream());

            server.AddHandler(new TextDocumentHandler(server));

            await server.Initialize();
            await server.WasShutDown;
        }
    }

    class TextDocumentHandler 
        : ITextDocumentSyncHandler 
        , IHoverHandler
        , IDefinitionHandler
        , IReferencesHandler
        , ICompletionHandler
        , IRenameHandler
        
    {
        private int maxNumberOfProblems = 100;

        VHDL_Library_Manager libraryManager = new VHDL_Library_Manager("", @"Libraries\LibraryRepository.xml", Logger.CreateLogger(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "compiler"));
        VhdlParserSettings settings = VhdlParserWrapper.DEFAULT_SETTINGS;
        RootDeclarativeRegion rootScope = new RootDeclarativeRegion();
        LibraryDeclarativeRegion currentLibrary = new LibraryDeclarativeRegion("work");

        private readonly ILanguageServer _router;
        public TextDocumentHandler(ILanguageServer router)
        {
            libraryManager.LoadData(@"Libraries");
            rootScope.Libraries.Add(currentLibrary);
            rootScope.Libraries.Add(libraryManager.GetLibrary("STD"));
            _router = router;
        }

        private readonly DocumentSelector _documentSelector = new DocumentSelector(
            new DocumentFilter()
            {
                Pattern = "**/*.vhd",
                Language = "vhdl"
            }
        );

        public TextDocumentSyncOptions Options { get; } = new TextDocumentSyncOptions()
        {
            WillSaveWaitUntil = false,
            WillSave = true,
            Change = TextDocumentSyncKind.Full,
            Save = new SaveOptions()
            {
                IncludeText = true
            },
            OpenClose = true
        };



        public TextDocumentAttributes GetTextDocumentAttributes(Uri uri)
        {
            _router.LogMessage(new LogMessageParams()
            {
                Type = MessageType.Log,
                Message = "GetTextDocumentAttributes " + uri.ToString()
            });
            return new TextDocumentAttributes(uri, "vhdl");
        }


        #region Change
        // --------------- Change ------------------------------------------
        public async Task Handle(DidChangeTextDocumentParams notification)
        {
            foreach (var change in notification.ContentChanges)
                VHDLParser(notification.TextDocument.Uri, change.Text);

        }

        TextDocumentChangeRegistrationOptions IRegistration<TextDocumentChangeRegistrationOptions>.GetRegistrationOptions()
        {
            return new TextDocumentChangeRegistrationOptions()
            {
                DocumentSelector = _documentSelector,
                SyncKind = Options.Change
            };
        }
        #endregion Change

        #region Open
        // --------------- Open ------------------------------------------
        public async Task Handle(DidOpenTextDocumentParams notification)
        {
            _router.LogMessage(new LogMessageParams()
            {
                Type = MessageType.Log,
                Message = "Hello World!"
            });
        }

        TextDocumentRegistrationOptions IRegistration<TextDocumentRegistrationOptions>.GetRegistrationOptions()
        {
            return new TextDocumentRegistrationOptions()
            {
                DocumentSelector = _documentSelector,
            };
        }
        #endregion Open


        #region Close
        // --------------- Close ------------------------------------------
        public Task Handle(DidCloseTextDocumentParams notification)
        {
            return Task.CompletedTask;
        }
        #endregion Close

        #region Save
        // --------------- Save ------------------------------------------
        public Task Handle(DidSaveTextDocumentParams notification)
        {
            return Task.CompletedTask;
        }

        TextDocumentSaveRegistrationOptions IRegistration<TextDocumentSaveRegistrationOptions>.GetRegistrationOptions()
        {
            return new TextDocumentSaveRegistrationOptions()
            {
                DocumentSelector = _documentSelector,
                IncludeText = Options.Save.IncludeText
            };
        }

        private SynchronizationCapability _SynchronizationCapability;

        public void SetCapability(SynchronizationCapability capability)
        {
            _SynchronizationCapability = capability;
        }

        #endregion Save

        #region Hover
        public Task<Hover> Handle(TextDocumentPositionParams request, CancellationToken token)
        {
            return Task.FromResult(new Hover());
        }

        private HoverCapability _HoverCapability;
        public void SetCapability(HoverCapability capability)
        {
            _HoverCapability = capability;
        }
        #endregion Hover


        #region Definition 
        Task<LocationOrLocations> IRequestHandler<TextDocumentPositionParams, LocationOrLocations>.Handle(TextDocumentPositionParams request, CancellationToken token)
        {
            return Task.FromResult(new LocationOrLocations());
        }

        DefinitionCapability _DefinitionCapability;
        public void SetCapability(DefinitionCapability capability)
        {
            _DefinitionCapability = capability;
        }
        #endregion

        #region References
        public Task<LocationContainer> Handle(ReferenceParams request, CancellationToken token)
        {
            return Task.FromResult(new LocationContainer());
        }

        ReferencesCapability _ReferencesCapability;
        public void SetCapability(ReferencesCapability capability)
        {
            _ReferencesCapability = capability;
        }
        #endregion References

        #region Completion
        Task<CompletionList> IRequestHandler<TextDocumentPositionParams, CompletionList>.Handle(TextDocumentPositionParams request, CancellationToken token)
        {
            return Task.FromResult(new CompletionList(Enumerable.Empty<CompletionItem>()));
        }

        public CompletionRegistrationOptions GetRegistrationOptions()
        {
            return new CompletionRegistrationOptions()
            {
                DocumentSelector = _documentSelector,
                TriggerCharacters = new Container<string>(new string[] { }),
                ResolveProvider = false
            };
        }

        CompletionCapability _CompletionCapability;
        public void SetCapability(CompletionCapability capability)
        {
            _CompletionCapability = capability;
        }
        #endregion Completion

        #region Rename Symbol
        public Task<WorkspaceEdit> Handle(RenameParams request, CancellationToken token)
        {
            return Task.FromResult(new WorkspaceEdit());
        }

        RenameCapability _RenameCapability;
        public void SetCapability(RenameCapability capability)
        {
            _RenameCapability = capability;
        }
        #endregion Rename Symbol

        /*
        Task INotificationHandler<DidChangeConfigurationParams>.Handle(DidChangeConfigurationParams notification)
        {
            return null;
        }

        public Task<DidChangeConfigurationCapability> ICapability<DidChangeConfigurationCapability>.SetCapability(DidChangeConfigurationCapability capability)
        {
            return null;
        }*/

        public void VHDLParser(Uri uri, string text)
        {
            bool success = true;
            string logMessage = "";
            var diagnostic = new List<Diagnostic>();
            try
            {
                logMessage += "Parsing code\n";
                var parsed = VHDL.parser.VhdlParserWrapper.parseString(text, settings, rootScope, currentLibrary, libraryManager);
            }
            catch (vhdlParseException ex)
            {
                diagnostic.Add(new Diagnostic()
                {
                    Code = new DiagnosticCode("ex"),
                    Message = ex.Message,
                    Range = new Lsp.Models.Range(new Position(ex.Line-1, ex.CharPositionInLine), new Position(ex.Line-1, ex.CharPositionInLine + ex.OffendingSymbol.Text.Length)),
                    Severity = DiagnosticSeverity.Warning,
                    Source = "ex"
                });

                logMessage += string.Format("{0} {1}:{2} {3} {4} {5}", ex.FilePath, ex.Line, ex.CharPositionInLine, ex.OffendingSymbol.Text, ex.Message, ex.InnerException) + "\nParsing failed\n";

                success = false;
            }
            catch (vhdlSemanticException ex)
            {
                logMessage += ex.GetConsoleMessageTest() + "\nParsing failed\n";
                success = false;
            }
            catch (Exception ex)
            {

                success = false;
            }

            var model = new PublishDiagnosticsParams()
            {
                Uri = uri,
                Diagnostics = diagnostic
            };
            Console.WriteLine(logMessage);
            _router.PublishDiagnostics(model);
        }

    }

}