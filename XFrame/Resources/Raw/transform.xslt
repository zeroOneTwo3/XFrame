<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
    <xsl:output method="xml" indent="yes"/>

    <xsl:key name="employee-group" match="item" use="concat(@name, '|', @surname)" />

    <xsl:template match="/Pay">
        <Employees>
            <xsl:for-each select="item[generate-id() = generate-id(key('employee-group', concat(@name, '|', @surname))[1])]">
                
                <Employee name="{@name}" surname="{@surname}">
                    <xsl:for-each select="key('employee-group', concat(@name, '|', @surname))">
                        <salary amount="{@amount}" mount="{@mount}"/>
                    </xsl:for-each>
                </Employee>
                
            </xsl:for-each>
        </Employees>
    </xsl:template>
</xsl:stylesheet>