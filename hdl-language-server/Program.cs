using System;
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
            var ipEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 5000);

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

    class TextDocumentHandler : ITextDocumentSyncHandler
    {
        private readonly ILanguageServer _router;
        private int maxNumberOfProblems = 100;


        private readonly DocumentSelector _documentSelector = new DocumentSelector(
            new DocumentFilter()
            {
                Pattern = "**/*.vhd",
                Language = "vhdl"
            }
        );

        private SynchronizationCapability _capability;

        public TextDocumentHandler(ILanguageServer router)
        {
            _router = router;
        }

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

        public Task Handle(DidChangeTextDocumentParams notification)
        {
            foreach (var change in notification.ContentChanges)
            {
                TestExample(notification.TextDocument.Uri, change.Text);
            }

            _router.LogMessage(new LogMessageParams()
            {
                Type = MessageType.Log,
                Message = "Hello!"
            });
            return Task.CompletedTask;
        }




        TextDocumentChangeRegistrationOptions IRegistration<TextDocumentChangeRegistrationOptions>.GetRegistrationOptions()
        {
            return new TextDocumentChangeRegistrationOptions()
            {
                DocumentSelector = _documentSelector,
                SyncKind = Options.Change
            };
        }

        public void SetCapability(SynchronizationCapability capability)
        {
            _capability = capability;
        }

        public async Task Handle(DidOpenTextDocumentParams notification)
        {
            TestExample(notification.TextDocument.Uri, notification.TextDocument.Text);
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

        public Task Handle(DidCloseTextDocumentParams notification)
        {
            return Task.CompletedTask;
        }

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
        public TextDocumentAttributes GetTextDocumentAttributes(Uri uri)
        {
            return new TextDocumentAttributes(uri, "vhdl");
        }

        public void TestExample(Uri uri, string Text)
        {
            var problems = 0;
            var diagnostic = new List<Diagnostic>();

            var lines = Regex.Split(Text.Replace("\t", ""), @"\r?\n");
            for (var i = 1; i < lines.Length; i++)
            {
                var line = lines[i];
                var matchs = Regex.Matches(line, "Entity");
                if (matchs.Count > 0)
                {
                    foreach (Match match in matchs)
                    {
                        problems++;
                        diagnostic.Add(new Diagnostic()
                        {
                            Code = new DiagnosticCode("ex"),
                            Message = $"{0} should be spelled entity",
                            Range = new Lsp.Models.Range(new Position(i, match.Index), new Position(i, match.Index + match.Length)),
                            Severity = DiagnosticSeverity.Warning,
                            Source = "ex"
                        });

                        if (problems > maxNumberOfProblems)
                            break;
                    }
                }


            }

            var model = new PublishDiagnosticsParams()
            {
                Uri = uri,
                Diagnostics = diagnostic
            };

            _router.PublishDiagnostics(model);

        }
    }
}