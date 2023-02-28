import { Component, Inject } from "@angular/core";
import { MAT_DIALOG_DATA } from "@angular/material/dialog";

@Component({ selector: "dialog-multiple-sheets", templateUrl: "multiplesheets.tmpl.html" })
export class MultipleSheetsDialog {
    sheetSel!: string
    constructor(@Inject(MAT_DIALOG_DATA) public data: { id: number, name: string }[]) { }
}
