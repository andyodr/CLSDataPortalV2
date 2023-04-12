import { Component, Inject, SecurityContext } from "@angular/core";
import { MAT_DIALOG_DATA } from "@angular/material/dialog";
import { DomSanitizer } from "@angular/platform-browser"

@Component({
    selector: "app-dialog",
    template: `
<h1 mat-dialog-title>{{data.title}}</h1>
<h2 mat-dialog-title>{{data.msg}}</h2>
<mat-dialog-content class="mat-typography" [innerHtml]="data.htmlContent"></mat-dialog-content>
<mat-dialog-actions>
<button *ngIf="data.alert" mat-raised-button mat-dialog-close>Close</button>
<button *ngIf="!data.alert" mat-raised-button mat-dialog-close>Cancel</button>
<button *ngIf="!data.alert" mat-raised-button [mat-dialog-close]="true">OK</button>
</mat-dialog-actions>`
})
export class AppDialog {
    constructor(
        @Inject(MAT_DIALOG_DATA)
        public data: { title: string, msg?: string, htmlContent?: string, alert?: boolean },
        private dom: DomSanitizer
    ) {
        if (data.htmlContent) {
            this.data.htmlContent = this.dom.sanitize(SecurityContext.HTML, data.htmlContent) ?? "unsanitary"
        }
    }
}
