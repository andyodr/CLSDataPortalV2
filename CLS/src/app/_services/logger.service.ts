import { Injectable } from "@angular/core"
import { MatSnackBar, MatSnackBarConfig } from "@angular/material/snack-bar"

type PanelClass = "snackbar-info" | "snackbar-warning" | "snackbar-success" | "snackbar-error"

@Injectable({ providedIn: "root" })
export class LoggerService {
    cfg: MatSnackBarConfig = {
        duration: 5000,
        horizontalPosition: "right",
        verticalPosition: "bottom"
    }

    constructor(private snackbar: MatSnackBar) { }

    logIt(message: string, panelClass: PanelClass) {
        this.snackbar.open(message, undefined, { panelClass: [panelClass], ...this.cfg })
    }

    logInfo(message: string) {
        this.logIt(message, "snackbar-info")
    }

    logWarning(message: string) {
        this.logIt(message, "snackbar-warning")
    }

    logSuccess(message: string) {
        this.logIt(message, "snackbar-success")
    }

    logError(message: string) {
        this.logIt(message, "snackbar-error")
    }
}
