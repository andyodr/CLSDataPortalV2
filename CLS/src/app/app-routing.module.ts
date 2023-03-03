import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { MeasureDefinitionComponent } from './measuredefinition/measuredefinition.component';
import { DataImportsComponent } from './dataimports/dataimports.component';
import { NotFoundComponent } from './errors/not-found/not-found.component';
import { ServerErrorComponent } from './errors/server-error/server-error.component';
import { TestErrorComponent } from './errors/test-error/test-error.component';
import { HomeComponent } from './home/home.component';
import { AuthGuard } from './_guards/auth.guard';
import { UserListComponent } from './users/user-list/user-list.component';

const routes: Routes = [
	{ path: '', loadChildren: () => import('./auth/auth.module').then(m => m.AuthModule) },
	{ path: 'measuredata', loadChildren: () => import('./measure-data/measure-data.module').then(m => m.MeasureDataModule) },
	{ path: 'users', component: UserListComponent },
    { path: 'targets', loadChildren: () => import('./target/target.module').then(m => m.TargetModule) },
    { path: 'measures', loadChildren: () => import('./measure/measure.module').then(m => m.MeasureModule) },
	{
		path: '',
		runGuardsAndResolvers: 'always',
		canActivate: [AuthGuard], children: [
			{ path: 'home', component: HomeComponent },
		]
	},
	{ path: 'measuredefinition', component: MeasureDefinitionComponent },
	{ path: "dataimports", title: "Distributor - Data Imports", component: DataImportsComponent },
	{ path: 'errors', component: TestErrorComponent },
	{ path: 'not-found', component: NotFoundComponent },
	{ path: 'server-error', component: ServerErrorComponent },
	{ path: '**', component: NotFoundComponent, pathMatch: 'full' }
];

@NgModule({
	imports: [RouterModule.forRoot(routes)],
	exports: [RouterModule]
})
export class AppRoutingModule { }
