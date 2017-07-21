/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */
'use strict';

import * as path from 'path';
import * as net from 'net';

import cp = require("child_process");
import { workspace, Disposable, ExtensionContext } from 'vscode';
import { LanguageClient, LanguageClientOptions, SettingMonitor, ServerOptions, ErrorAction, ErrorHandler, CloseAction, TransportKind } from 'vscode-languageclient';


function startLangServer(command: string, documentSelector: string[]): Disposable {

    let debugOptions = { execArgv: ["--nolazy", "--debug=6009"] };
    const serverOptions: ServerOptions = {
        run: { command: command, transport: TransportKind.stdio },
        debug: { command: command, transport: TransportKind.stdio,} //options: debugOptions }
    };
    const clientOptions: LanguageClientOptions = {
        documentSelector: documentSelector,
        synchronize: {
            // Synchronize the setting section 'hdlLanguageServer' to the server
            configurationSection: 'hdlLanguageServer',
            // Notify the server about file changes to '.clientrc files contain in the workspace
            fileEvents: workspace.createFileSystemWatcher('**/.clientrc')
        },
    }
    return new LanguageClient(command, serverOptions, clientOptions).start();
}

function startLangServerTCP(addr: number, documentSelector: string[]): Disposable {
    const serverOptions: ServerOptions = function () {
        return new Promise((resolve, reject) => {
            var client = new net.Socket();
            client.connect(addr, "127.0.0.1", function () {
                resolve({
                    reader: client,
                    writer: client
                });
            });
        });
    }

    const clientOptions: LanguageClientOptions = {
        documentSelector: documentSelector,
        synchronize: {
            // Synchronize the setting section 'hdlLanguageServer' to the server
            configurationSection: 'hdlLanguageServer',
            // Notify the server about file changes to '.clientrc files contain in the workspace
            fileEvents: workspace.createFileSystemWatcher('**/.clientrc')
        },
    }
    return new LanguageClient(`tcp lang server (port ${addr})`, serverOptions, clientOptions).start();
}

export function activate(context: ExtensionContext) {
    //let serverExe = context.asAbsolutePath('./server/hdl-language-server.exe');
    //context.subscriptions.push(startLangServer(serverExe, ["vhdl"]));
    // For TCP
    context.subscriptions.push(startLangServerTCP(5001, ["vhdl"]));
}
