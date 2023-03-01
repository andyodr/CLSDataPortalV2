import { NgModule } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
import { HttpClientModule, HTTP_INTERCEPTORS } from "@angular/common/http";
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';
import { AppRoutingModule } from './app-routing.module';
import { AppComponent } from './app.component';
import { FormsModule } from '@angular/forms';
import { HomeComponent } from './home/home.component';
import { SharedModule } from './_modules/shared.module';
import { TestErrorComponent } from './errors/test-error/test-error.component';
import { ErrorInterceptor } from './_interceptors/error.interceptor';
import { NotFoundComponent } from './errors/not-found/not-found.component';
import { ServerErrorComponent } from './errors/server-error/server-error.component';
import { DataImportsComponent } from './dataimports/dataimports.component';
import { ErrorsComponent } from './errors/errors.component';
import { TableComponent } from './table/table.component';
import { FilterPipe } from './filter.pipe';
import { NgbModule } from '@ng-bootstrap/ng-bootstrap';
import { MatDialogModule } from '@angular/material/dialog'
import { MatRadioModule } from '@angular/material/radio'
import { MatSelectModule } from '@angular/material/select'
import { MatProgressBarModule } from '@angular/material/progress-bar'
import { MultipleSheetsDialog } from './dataimports/multiplesheets-dialog.component'
import { AppDialog } from './app-dialog.component'
import { UserListComponent } from './users/user-list/user-list.component';
import { LoginComponent } from './auth/login/login.component';
import { AuthModule } from './auth/auth.module';
import { MeasureDataModule } from './measure-data/measure-data.module';
import { TargetModule } from './target/target.module';

@NgModule({
    declarations: [
        AppComponent,
        HomeComponent,
        DataImportsComponent,
        TestErrorComponent,
        NotFoundComponent,
        ServerErrorComponent,
        ErrorsComponent,
        TableComponent,
        FilterPipe,
        MultipleSheetsDialog,
        AppDialog,
        UserListComponent
    ],
    imports: [
        BrowserAnimationsModule,
        BrowserModule,
        AppRoutingModule,
        HttpClientModule,
        FormsModule,
        SharedModule,
        AuthModule,
        MeasureDataModule,
        TargetModule,
        NgbModule,
        MatDialogModule,
        MatRadioModule,
        MatSelectModule,
        MatProgressBarModule
    ],
    providers: [
        { provide: HTTP_INTERCEPTORS, useClass: ErrorInterceptor, multi: true },
    ],
    bootstrap: [AppComponent]
})
export class AppModule { }
