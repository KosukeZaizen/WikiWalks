import * as React from "react";
import { Link } from "react-router-dom";
import { Button } from "reactstrap";
import Head from "./Helmet";

const NotFound = () => {
    const params = getParams();

    return (
        <div>
            <Head title="404" noindex={true} />
            <div className="center">
                <h1>Page not found!</h1>
                <hr />
                <h2>
                    No match for <code>{params && params["p"]}</code>
                </h2>
                <p>Please check if the url is correct!</p>
                <Link to="/">
                    <Button color="primary" style={{ width: "50%" }}>
                        <b>Home >></b>
                    </Button>
                </Link>
            </div>
        </div>
    );
};

function getParams() {
    let arg = {};
    const pair = window.location.search.substring(1).split("&");
    for (let i = 0; pair[i]; i++) {
        const kv = pair[i].split("=");
        arg[kv[0]] = kv[1];
    }
    return arg;
}

export default NotFound;
