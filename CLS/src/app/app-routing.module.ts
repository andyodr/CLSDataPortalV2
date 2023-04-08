import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { MeasureDataComponent } from './measure-data/measure-data.component';
import { MeasureDefinitionComponent } from './measuredefinition/measuredefinition.component';
import { MeasureDefinitionEditComponent } from "./measuredefinition/measuredefinition-edit.component"
import { DataImportsComponent } from './dataimports/dataimports.component';
import { NotFoundComponent } from './errors/not-found/not-found.component';
import { ServerErrorComponent } from './errors/server-error/server-error.component';
import { TestErrorComponent } from './errors/test-error/test-error.component';
import { HomeComponent } from './home/home.component';
import { AuthGuard } from './_guards/auth.guard';
import { CalendarSettingsComponent as CalendarSettingsComponent } from "./calendar/settings.component"
import { UserListComponent } from "./users/userlist.component"
import { UserAddComponent } from "./users/useradd.component"
import { UserEditComponent } from "./users/useredit.component"
import { RegionHierarchyComponent } from "./hierarchy/hierarchy.component"
import { TargetsComponent } from './targets/targets.component';
import { MeasuresComponent } from './measures/measures.component';

const title = "DELIVER - "

const routes: Routes = [
    { path: '', loadChildren: () => import('./auth/auth.module').then(m => m.AuthModule) },
    { path: 'measuredata', title: `${ title }Measure Data`, component: MeasureDataComponent },
    {
        path: "users", children: [
            { path: "add", title: `${ title }Add User`, component: UserAddComponent },
            { path: "", title: `${ title }Users`, component: UserListComponent },
            { path: ":id", title: `${ title }Edit User`, component: UserEditComponent }
        ]
    },
    { path: "settings", title: `${ title }Settings`, component: CalendarSettingsComponent },
    { path: "hierarchy", title: `${ title }Region Hierarchy`, component: RegionHierarchyComponent },
    {
        path: '',
        runGuardsAndResolvers: 'always',
        canActivate: [AuthGuard], children: [
            { path: 'home', component: HomeComponent },
        ]
    },
    { path: 'targets', title: `${ title }Targets`, component: TargetsComponent },
    { path: 'measures', title: `${ title }Measures`, component: MeasuresComponent },
    {
        path: "measuredefinition", children: [
            { path: "", title: `${ title }Measure Definitions`, component: MeasureDefinitionComponent },
            { path: "add", title: `${ title }Add Measure Definition`, component: MeasureDefinitionEditComponent },
            { path: ":id", title: `${ title }Edit Measure Definition`, component: MeasureDefinitionEditComponent },
        ]
    },
    { path: "dataimports", title: `${ title }Data Imports`, component: DataImportsComponent },
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
