import { HttpClientModule, HTTP_INTERCEPTORS } from "@angular/common/http";
import { NgModule } from "@angular/core"
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from "@angular/material/button"
import { MatDialogModule } from '@angular/material/dialog'
import { MatFormFieldModule } from "@angular/material/form-field";
import { MatIconModule } from '@angular/material/icon'
import { MatInputModule } from "@angular/material/input"
import { MatProgressBarModule } from '@angular/material/progress-bar'
import { MatRadioModule } from '@angular/material/radio';
import { MatSelectModule } from '@angular/material/select';
import { MatSnackBarModule } from '@angular/material/snack-bar';
import { BrowserModule } from '@angular/platform-browser';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';
import { NgbModule } from '@ng-bootstrap/ng-bootstrap';
import { AppDialog } from './app-dialog.component';
import { AppRoutingModule } from './app-routing.module';
import { AppComponent } from './app.component';
import { DataImportsComponent } from './dataimports/dataimports.component';
import { MultipleSheetsDialog } from './dataimports/multiplesheets-dialog.component';
import { UploadDirective } from "./dataimports/upload.directive"
import { ErrorsComponent } from './errors/errors.component';
import { NotFoundComponent } from './errors/not-found/not-found.component';
import { ServerErrorComponent } from './errors/server-error/server-error.component';
import { TestErrorComponent } from './errors/test-error/test-error.component';
import { FilterPipe } from './filter.pipe';
import { HomeComponent } from './home/home.component';
import { MeasureDefinitionComponent } from './measuredefinition/measuredefinition.component';
import { TableComponent } from './table/table.component';
import { UserListComponent } from "./users/userlist.component"
import { ErrorInterceptor } from './_interceptors/error.interceptor';
import { SharedModule } from './_modules/shared.module';
import { UserEditComponent } from "./users/useredit.component"
import { UserAddComponent } from "./users/useradd.component"

@NgModule({
    declarations: [
        AppComponent,
        AppDialog,
        DataImportsComponent,
        ErrorsComponent,
        FilterPipe,
        HomeComponent,
        MeasureDefinitionComponent,
        MultipleSheetsDialog,
        NotFoundComponent,
        ServerErrorComponent,
        TableComponent,
        TestErrorComponent,
        UploadDirective,
        UserListComponent,
        UserEditComponent,
        UserAddComponent
    ],
    imports: [
        AppRoutingModule,
        BrowserAnimationsModule,
        BrowserModule,
        FormsModule,
        HttpClientModule,
        MatButtonModule,
        MatDialogModule,
        MatFormFieldModule,
        MatIconModule,
        MatInputModule,
        MatProgressBarModule,
        MatRadioModule,
        MatSelectModule,
        MatSnackBarModule,
        NgbModule,
        SharedModule
    ],
    providers: [
        { provide: HTTP_INTERCEPTORS, useClass: ErrorInterceptor, multi: true },
    ],
    bootstrap: [AppComponent]
})
export class AppModule { }
