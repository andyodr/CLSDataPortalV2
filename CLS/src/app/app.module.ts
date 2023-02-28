import { HttpClientModule, HTTP_INTERCEPTORS } from "@angular/common/http";
import { NgModule } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatDialogModule } from '@angular/material/dialog';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatRadioModule } from '@angular/material/radio';
import { MatSelectModule } from '@angular/material/select';
import { BrowserModule } from '@angular/platform-browser';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';
import { NgbModule } from '@ng-bootstrap/ng-bootstrap';
import { AppDialog } from './app-dialog.component';
import { AppRoutingModule } from './app-routing.module';
import { AppComponent } from './app.component';
import { DataImportsComponent } from './dataimports/dataimports.component';
import { MultipleSheetsDialog } from './dataimports/multiplesheets-dialog.component';
import { ErrorsComponent } from './errors/errors.component';
import { NotFoundComponent } from './errors/not-found/not-found.component';
import { ServerErrorComponent } from './errors/server-error/server-error.component';
import { TestErrorComponent } from './errors/test-error/test-error.component';
import { FilterPipe } from './filter.pipe';
import { HierarchyComponent } from './hierarchy/hierarchy.component';
import { HomeComponent } from './home/home.component';
import { ListsComponent } from './lists/lists.component';
import { MeasureDefinitionComponent } from './measuredefinition/measuredefinition.component';
import { MeasuresComponent } from './measures/measures.component';
import { MemberDetailComponent } from './members/member-detail/member-detail.component';
import { MemberListComponent } from './members/member-list/member-list.component';
import { MessagesComponent } from './messages/messages.component';
import { NavComponent } from './nav/nav.component';
import { RegisterComponent } from './register/register.component';
import { SideComponent } from './side/side.component';
import { TableComponent } from './table/table.component';
import { TargetsComponent } from './targets/targets.component';
import { UserListComponent } from './users/user-list/user-list.component';
import { ErrorInterceptor } from './_interceptors/error.interceptor';
import { SharedModule } from './_modules/shared.module';

@NgModule({
	declarations: [
		AppComponent,
		AppDialog,
		DataImportsComponent,
		ErrorsComponent,
		FilterPipe,
		HierarchyComponent,
		HomeComponent,
		ListsComponent,
		MeasureDefinitionComponent,
		MeasuresComponent,
		MemberDetailComponent,
		MemberListComponent,
		MessagesComponent,
		MultipleSheetsDialog,
		NavComponent,
		NotFoundComponent,
		RegisterComponent,
		ServerErrorComponent,
		SideComponent,
		TableComponent,
		TargetsComponent,
		TestErrorComponent,
		UserListComponent
	],
	imports: [
		AppRoutingModule,
		BrowserAnimationsModule,
		BrowserModule,
		FormsModule,
		HttpClientModule,
		MatDialogModule,
		MatProgressBarModule,
		MatRadioModule,
		MatSelectModule,
		NgbModule,
		SharedModule
	],
	providers: [
		{ provide: HTTP_INTERCEPTORS, useClass: ErrorInterceptor, multi: true },
	],
	bootstrap: [AppComponent]
})
export class AppModule { }
