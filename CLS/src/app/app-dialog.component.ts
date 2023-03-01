import { Component, Inject } from "@angular/core";
import { MAT_DIALOG_DATA } from "@angular/material/dialog";

@Component({
    selector: "app-dialog",
    template: `
<h1 mat-dialog-title>{{data.title}}</h1>
<p mat-dialog-content>{{data.message}}</p>
<form mat-dialog-actions>
<button *ngIf="data.alert" mat-raised-button mat-dialog-close>Close</button>
<button *ngIf="!data.alert" mat-raised-button mat-dialog-close>Cancel</button>
<button *ngIf="!data.alert" mat-raised-button [mat-dialog-close]="true">OK</button>
</form>`
})
export class AppDialog {
    constructor(
        @Inject(MAT_DIALOG_DATA)
        public data: { title: string, message: string, alert?: boolean }) { }
}
