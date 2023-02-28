import { NgModule } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
import { HttpClientModule, HTTP_INTERCEPTORS } from "@angular/common/http";
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';
import { AppRoutingModule } from './app-routing.module';
import { AppComponent } from './app.component';
import { NavComponent } from './nav/nav.component';
import { FormsModule } from '@angular/forms';
import { HomeComponent } from './home/home.component';
import { RegisterComponent } from './register/register.component';
import { MemberListComponent } from './members/member-list/member-list.component';
import { MemberDetailComponent } from './members/member-detail/member-detail.component';
import { ListsComponent } from './lists/lists.component';
import { MessagesComponent } from './messages/messages.component';
import { SharedModule } from './_modules/shared.module';
import { TestErrorComponent } from './errors/test-error/test-error.component';
import { ErrorInterceptor } from './_interceptors/error.interceptor';
import { NotFoundComponent } from './errors/not-found/not-found.component';
import { ServerErrorComponent } from './errors/server-error/server-error.component';
import { SideComponent } from './side/side.component';
import { DataImportsComponent } from './dataimports/dataimports.component';
import { TargetsComponent } from './targets/targets.component';
import { MeasuresComponent } from './measures/measures.component';
import { MeasureDefinitionComponent } from './measuredefinition/measuredefinition.component';
import { HierarchyComponent } from './hierarchy/hierarchy.component';
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

@NgModule({
    declarations: [
        AppComponent,
        NavComponent,
        HomeComponent,
        RegisterComponent,
        MemberListComponent,
        MemberDetailComponent,
        ListsComponent,
        MessagesComponent,
        MeasureDataComponent,
        TargetsComponent,
        MeasuresComponent,
        MeasureDefinitionComponent,
        HierarchyComponent,
        DataImportsComponent,
        TestErrorComponent,
        NotFoundComponent,
        ServerErrorComponent,
        SideComponent,
        LoginComponent,
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
