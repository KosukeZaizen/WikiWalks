import React from "react";
import { Route, Switch } from "react-router";
import NotFound from "./components/404";
import All from "./components/All";
import Category from "./components/Category";
import Layout from "./components/Layout";
import ScrollToTop from "./components/ScrollToTop";
import Top from "./components/Top";
import Word from "./components/Word";

export default () => (
    <Layout>
        <ScrollToTop>
            <Switch>
                <Route sensitive exact path="/" component={Top} />
                <Route sensitive exact path="/all" component={All} />
                <Route
                    sensitive
                    path="/category/:category"
                    component={Category}
                />
                <Route sensitive path="/word/:wordId" component={Word} />
                <Route sensitive exact path="/not-found" component={NotFound} />
                <Route component={NotFoundRedirect} />
            </Switch>
        </ScrollToTop>
    </Layout>
);

function NotFoundRedirect() {
    window.location.href = `/not-found?p=${window.location.pathname}`;
    return <div>Loading...</div>;
}
