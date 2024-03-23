import { enableProdMode, importProvidersFrom } from "@angular/core"
import { environment } from "./environments/environment"
import { AppComponent } from "./app/app.component"
import { BsDropdownModule } from "ngx-bootstrap/dropdown"
import { NgbModule } from "@ng-bootstrap/ng-bootstrap"
import { MatTreeModule } from "@angular/material/tree"
import { MatTooltipModule } from "@angular/material/tooltip"
import { MatTableModule } from "@angular/material/table"
import { MatSortModule } from "@angular/material/sort"
import { MatSnackBarModule } from "@angular/material/snack-bar"
import { MatSidenavModule } from "@angular/material/sidenav"
import { MatSelectModule } from "@angular/material/select"
import { MatRippleModule } from "@angular/material/core"
import { MatRadioModule } from "@angular/material/radio"
import { MatProgressBarModule } from "@angular/material/progress-bar"
import { MatMenuModule } from "@angular/material/menu"
import { MatInputModule } from "@angular/material/input"
import { MatIconModule } from "@angular/material/icon"
import { MatFormFieldModule } from "@angular/material/form-field"
import { MatDialogModule } from "@angular/material/dialog"
import { MatCheckboxModule } from "@angular/material/checkbox"
import { MatButtonModule } from "@angular/material/button"
import { AppRoutingModule } from "./app/app-routing.module"
import { FormsModule, ReactiveFormsModule } from "@angular/forms"
import { BrowserModule, bootstrapApplication, provideClientHydration } from "@angular/platform-browser"
import { provideAnimations } from "@angular/platform-browser/animations"
import { ErrorInterceptor } from "./app/_interceptors/error.interceptor"
import { HTTP_INTERCEPTORS, withInterceptorsFromDi, provideHttpClient } from "@angular/common/http"

if (environment.production) {
  enableProdMode()
}

bootstrapApplication(AppComponent, {
    providers: [
        importProvidersFrom(BrowserModule, FormsModule, AppRoutingModule, MatButtonModule, MatCheckboxModule,
            MatDialogModule, MatFormFieldModule, MatIconModule, MatInputModule, MatMenuModule, MatProgressBarModule,
            MatRadioModule, MatRippleModule, MatSelectModule, MatSidenavModule, MatSnackBarModule, MatSortModule,
            MatTableModule, MatTooltipModule, MatTreeModule, NgbModule, ReactiveFormsModule, BsDropdownModule.forRoot()),
        { provide: HTTP_INTERCEPTORS, useClass: ErrorInterceptor, multi: true },
        provideAnimations(),
        provideHttpClient(withInterceptorsFromDi()), provideClientHydration()
    ]
}).catch(err => console.error(err))
