import React from "react";
import { Link } from "react-router-dom";
import { Container, Navbar, NavbarBrand } from "reactstrap";
import "./NavMenu.css";

export default function NavMenu() {
    return (
        <header>
            <Navbar
                className="navbar-expand-sm navbar-toggleable-sm border-bottom box-shadow mb-3"
                light
            >
                <Container>
                    <NavbarBrand
                        tag={Link}
                        to="/"
                        style={{ fontWeight: "bold" }}
                    >
                        Wiki Ninja
                    </NavbarBrand>
                </Container>
            </Navbar>
        </header>
    );
}
