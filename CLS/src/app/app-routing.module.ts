import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { MeasureDataComponent } from './measure-data/measure-data.component';
import { MeasureDefinitionComponent } from './measuredefinition/measuredefinition.component';
import { DataImportsComponent } from './dataimports/dataimports.component';
import { NotFoundComponent } from './errors/not-found/not-found.component';
import { ServerErrorComponent } from './errors/server-error/server-error.component';
import { TestErrorComponent } from './errors/test-error/test-error.component';
import { HomeComponent } from './home/home.component';
import { AuthGuard } from './_guards/auth.guard';
import { UserListComponent } from "./users/userlist.component"
import { UserAddComponent } from "./users/useradd.component"
import { UserEditComponent } from "./users/useredit.component"
import { RegionHierarchyComponent } from "./hierarchy/hierarchy.component"
import { TargetsComponent } from './targets/targets.component';
import { MeasuresComponent } from './measures/measures.component';

const routes: Routes = [
    { path: '', loadChildren: () => import('./auth/auth.module').then(m => m.AuthModule) },
    { path: 'measuredata', component: MeasureDataComponent },
    {
        path: "users", children: [
            { path: "add", title: "Deliver Data Portal - Add User", component: UserAddComponent },
            { path: "", title: "Deliver Data Portal - Users", component: UserListComponent },
            { path: ":id", title: "Deliver Data Portal - Edit User", component: UserEditComponent }
        ]
    },
    { path: "hierarchy", title: "Deliver Data Portal - Region Hierarchy", component: RegionHierarchyComponent },
    {
        path: '',
        runGuardsAndResolvers: 'always',
        canActivate: [AuthGuard], children: [
            { path: 'home', component: HomeComponent },
        ]
    },
    { path: 'targets', component: TargetsComponent },
    { path: 'measures', component: MeasuresComponent },
    { path: 'measuredefinition', component: MeasureDefinitionComponent },
    { path: "dataimports", title: "Deliver Data Portal - Data Imports", component: DataImportsComponent },
    { path: 'errors', component: TestErrorComponent },
    { path: 'not-found', component: NotFoundComponent },
    { path: 'server-error', component: ServerErrorComponent },
    { path: '**', component: NotFoundComponent, pathMatch: 'full' }
];
//{ path: 'measuredata', loadChildren: () => import('./measure-data/measure-data.module').then(m => m.MeasureDataModule) },

@NgModule({
    imports: [RouterModule.forRoot(routes)],
    exports: [RouterModule]
})
export class AppRoutingModule { }
