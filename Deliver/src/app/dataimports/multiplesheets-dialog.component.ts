import { Component, Inject } from "@angular/core"
import { MAT_DIALOG_DATA, MatDialogModule } from "@angular/material/dialog"
import { MatButtonModule } from "@angular/material/button"
import { FormsModule } from "@angular/forms"
import { MatRadioModule } from "@angular/material/radio"

@Component({
    selector: "dialog-multiple-sheets",
    templateUrl: "multiplesheets.tmpl.html",
    standalone: true,
    imports: [MatDialogModule, MatRadioModule, FormsModule, MatButtonModule]
})
export class MultipleSheetsDialog {
    sheetSel!: string
    constructor(@Inject(MAT_DIALOG_DATA) public data: { id: number, name: string }[]) { }
}
