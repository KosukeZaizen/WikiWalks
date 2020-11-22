import "bootstrap/dist/css/bootstrap.css";
import { createBrowserHistory } from "history";
import React from "react";
import ReactDOM from "react-dom";
import { Provider } from "react-redux";
import { ConnectedRouter } from "react-router-redux";
import App from "./App";
import { azureUrl, siteUrl } from "./common/consts";
import registerServiceWorker from "./registerServiceWorker";
import configureStore from "./store/configureStore";

// Create browser history to use in the Redux store
const baseUrl = document.getElementsByTagName("base")[0].getAttribute("href");
const history = createBrowserHistory({ basename: baseUrl });

//AzureUrlから通常のURLへリダイレクト
if (window.location.href.includes(azureUrl))
    window.location.href = window.location.href.replace(azureUrl, siteUrl);

// Get the application-wide store instance, prepopulating with state from the server where available.
const initialState = window.initialReduxState;
const store = configureStore(history, initialState);

const rootElement = document.getElementById("root");

ReactDOM.render(
    <Provider store={store}>
        <ConnectedRouter history={history}>
            <App />
        </ConnectedRouter>
    </Provider>,
    rootElement
);

registerServiceWorker();
