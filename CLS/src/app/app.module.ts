import { HttpClientModule, HTTP_INTERCEPTORS } from "@angular/common/http";
import { NgModule } from "@angular/core"
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from "@angular/material/button"
import { MatCheckboxModule } from "@angular/material/checkbox"
import { MatDialogModule } from '@angular/material/dialog'
import { MatFormFieldModule } from "@angular/material/form-field";
import { MatIconModule } from "@angular/material/icon"
import { MatInputModule } from "@angular/material/input"
import { MatMenuModule } from "@angular/material/menu";
import { MatProgressBarModule } from "@angular/material/progress-bar"
import { MatRadioModule } from '@angular/material/radio';
import { MatSidenavModule } from "@angular/material/sidenav"
import { MatSelectModule } from '@angular/material/select';
import { MatSnackBarModule } from "@angular/material/snack-bar"
import { MatSortModule } from "@angular/material/sort"
import { MatTableModule } from "@angular/material/table"
import { MatTooltipModule } from "@angular/material/tooltip"
import { MatTreeModule } from "@angular/material/tree"
import { BrowserModule } from '@angular/platform-browser';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';
import { NgbModule } from '@ng-bootstrap/ng-bootstrap';
import { AppDialog } from './app-dialog.component';
import { AppRoutingModule } from './app-routing.module';
import { AppComponent } from './app.component';
import { BsDropdownModule } from "ngx-bootstrap/dropdown"
import { DataImportsComponent } from './dataimports/dataimports.component';
import { ErrorInterceptor } from './_interceptors/error.interceptor';
import { ErrorsComponent } from './errors/errors.component';
import { FilterPipe } from "./lib/filter.pipe"
import { HomeComponent } from './home/home.component';
import { MeasureDataComponent } from "./measure-data/measure-data.component"
import { MeasureDefinitionComponent } from './measuredefinition/measuredefinition.component';
import { MeasuresComponent } from './measures/measures.component'
import { MultipleSheetsDialog } from './dataimports/multiplesheets-dialog.component';
import { NavbarComponent } from "./nav/topbar.component"
import { NavigateBackDirective } from "./_services/nav.service"
import { NotFoundComponent } from './errors/not-found/not-found.component';
import { RegionTreeComponent } from "./lib/region-tree/region-tree.component"
import { RegionHierarchyComponent } from "./hierarchy/hierarchy.component"
import { SbComponent } from "./_services/logger.service"
import { ServerErrorComponent } from './errors/server-error/server-error.component';
import { SidebarComponent } from "./nav/sidebar.component"
import { TableComponent } from "./lib/table/table.component"
import { TargetsComponent } from './targets/targets.component';
import { TestErrorComponent } from './errors/test-error/test-error.component';
import { UploadDirective } from "./dataimports/upload.directive"
import { UserAddComponent } from "./users/useradd.component"
import { UserEditComponent } from "./users/useredit.component"
import { UserListComponent } from "./users/userlist.component"

@NgModule({
    declarations: [
        AppComponent,
        AppDialog,
        DataImportsComponent,
        ErrorsComponent,
        FilterPipe,
        HomeComponent,
        MeasureDataComponent,
        MeasureDefinitionComponent,
        MeasuresComponent,
        MultipleSheetsDialog,
        NavbarComponent,
        NavigateBackDirective,
        NotFoundComponent,
        RegionHierarchyComponent,
        RegionTreeComponent,
        SbComponent,
        ServerErrorComponent,
        SidebarComponent,
        TableComponent,
        TargetsComponent,
        TestErrorComponent,
        UploadDirective,
        UserAddComponent,
        UserEditComponent,
        UserListComponent
    ],
    imports: [
        AppRoutingModule,
        BrowserAnimationsModule,
        BrowserModule,
        FormsModule,
        HttpClientModule,
        MatButtonModule,
        MatCheckboxModule,
        MatDialogModule,
        MatSidenavModule,
        MatFormFieldModule,
        MatIconModule,
        MatInputModule,
        MatMenuModule,
        MatProgressBarModule,
        MatRadioModule,
        MatSelectModule,
        MatSnackBarModule,
        MatSortModule,
        MatTableModule,
        MatTooltipModule,
        MatTreeModule,
        NgbModule,
        BsDropdownModule.forRoot(),

    ],
    providers: [
        { provide: HTTP_INTERCEPTORS, useClass: ErrorInterceptor, multi: true },
    ],
    bootstrap: [AppComponent]
})
export class AppModule { }
