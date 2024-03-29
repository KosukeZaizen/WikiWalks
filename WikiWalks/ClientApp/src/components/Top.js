import React, { Component } from "react";
import { connect } from "react-redux";
import { Link } from "react-router-dom";
import { Button } from "reactstrap";
import { bindActionCreators } from "redux";
import { actionCreators } from "../store/WikiWalks";
import Head from "./Helmet";

class Top extends Component {
    unmounted = false;

    constructor(props) {
        super(props);

        this.state = {
            categories: [],
        };
    }

    componentDidMount() {
        const getData = async () => {
            const url = `api/WikiWalks/getJapaneseCategories`;
            const response = await fetch(url);
            const categories = await response.json();

            this.setState({ categories });
        };
        getData();
    }

    componentWillUnmount() {
        this.unmounted = true;
    }

    render() {
        const { categories } = this.state;
        return (
            <div>
                <Head
                    title={"Japan Info"}
                    desc={
                        "This is a website to list information about Japan! You can check a lot of information about Japan!"
                    }
                    noad
                />
                <h1>Welcome to Japan Info!</h1>
                <br />
                <p>
                    This is a website to list information about Japan!
                    <br />
                    You can check a lot of information about Japan!
                    <br />
                    Information is from Wikipedia!
                </p>
                <br />
                <table className="table table-striped">
                    <thead>
                        <tr>
                            <th style={{ verticalAlign: "middle" }}>
                                Category Name
                            </th>
                            <th>
                                <span
                                    style={{
                                        display: "inline-block",
                                        minWidth: 100,
                                    }}
                                >
                                    Number of Keywords
                                </span>
                            </th>
                        </tr>
                    </thead>
                    <tbody>
                        {categories.length > 0 ? (
                            categories.map(category => (
                                <tr key={category.category}>
                                    <td>
                                        {
                                            <Link
                                                to={
                                                    "/category/" +
                                                    encodeURIComponent(
                                                        category.category
                                                            .split(" ")
                                                            .join("_")
                                                    )
                                                }
                                            >
                                                {category.category}
                                            </Link>
                                        }
                                    </td>
                                    <td>{category.cnt} keywords</td>
                                </tr>
                            ))
                        ) : (
                            <tr>
                                <td>Loading...</td>
                                <td></td>
                            </tr>
                        )}
                    </tbody>
                </table>
                {categories.length > 0 && categories.length < 200 && (
                    <center>Loading...</center>
                )}
                <hr />
                <Link to="/all">
                    <center>
                        <Button>
                            <b>{"Check all keywords"}</b>
                        </Button>
                    </center>
                </Link>
                <br />
            </div>
        );
    }
}

export default connect(
    state => state.wikiWalks,
    dispatch => bindActionCreators(actionCreators, dispatch)
)(Top);
