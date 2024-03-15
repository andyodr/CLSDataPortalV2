import { Component, Inject, SecurityContext } from "@angular/core";
import { MAT_DIALOG_DATA, MatDialogModule } from "@angular/material/dialog";
import { DomSanitizer } from "@angular/platform-browser"
import { MatButtonModule } from "@angular/material/button";

@Component({
    selector: "app-dialog",
    template: `
<h1 mat-dialog-title>{{data.title}}</h1>
<h2 mat-dialog-title>{{data.msg}}</h2>
<mat-dialog-content class="mat-typography" [innerHtml]="data.htmlContent"></mat-dialog-content>
<mat-dialog-actions>
  @if (data.alert) {
    <button mat-raised-button mat-dialog-close>Close</button>
  }
  @if (!data.alert) {
    <button mat-raised-button mat-dialog-close>Cancel</button>
  }
  @if (!data.alert) {
    <button mat-raised-button [mat-dialog-close]="true">OK</button>
  }
</mat-dialog-actions>`,
    standalone: true,
    imports: [MatDialogModule, MatButtonModule]
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
